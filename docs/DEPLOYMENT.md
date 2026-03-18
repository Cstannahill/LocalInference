# Deployment Guide

Production deployment instructions for LocalInference API.

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Docker Deployment](#docker-deployment)
3. [Kubernetes Deployment](#kubernetes-deployment)
4. [Cloud Deployment](#cloud-deployment)
5. [Reverse Proxy Setup](#reverse-proxy-setup)
6. [SSL/TLS Configuration](#ssltls-configuration)
7. [Monitoring](#monitoring)
8. [Backup and Recovery](#backup-and-recovery)
9. [Scaling](#scaling)
10. [Security Hardening](#security-hardening)

## Prerequisites

### Production Requirements

- **PostgreSQL 14+** with pgvector extension
- **Redis** (optional, for caching)
- **Docker** 20.10+ or container runtime
- **Reverse Proxy** (Nginx, Traefik, or Caddy)
- **SSL Certificates** (Let's Encrypt or commercial)

### Resource Requirements

| Component         | CPU      | Memory | Storage |
| ----------------- | -------- | ------ | ------- |
| API Server        | 2 cores  | 4GB    | 10GB    |
| PostgreSQL        | 2 cores  | 4GB    | 50GB+   |
| Ollama (optional) | 4+ cores | 16GB+  | 50GB+   |

## Docker Deployment

### Single Container

Create `Dockerfile`:

```dockerfile
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["LocalInference.sln", "./"]
COPY ["src/LocalInference.Domain/LocalInference.Domain.csproj", "src/LocalInference.Domain/"]
COPY ["src/LocalInference.Application/LocalInference.Application.csproj", "src/LocalInference.Application/"]
COPY ["src/LocalInference.Infrastructure/LocalInference.Infrastructure.csproj", "src/LocalInference.Infrastructure/"]
COPY ["src/LocalInference.Api/LocalInference.Api.csproj", "src/LocalInference.Api/"]

# Restore dependencies
RUN dotnet restore "src/LocalInference.Api/LocalInference.Api.csproj"

# Copy source code
COPY . .

# Build and publish
WORKDIR "/src/src/LocalInference.Api"
RUN dotnet publish -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Create non-root user
RUN adduser --disabled-password --gecos "" appuser

# Copy published files
COPY --from=build /app/publish .

# Set permissions
RUN chown -R appuser:appuser /app
USER appuser

# Expose port
EXPOSE 8080

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "LocalInference.Api.dll"]
```

Build and run:

```bash
# Build image
docker build -t localinference:latest .

# Run container
docker run -d \
  --name localinference \
  -p 5000:8080 \
  -e ConnectionStrings__DefaultConnection="Host=postgres;Database=LocalInference;Username=postgres;Password=secret" \
  -e Inference__Ollama__BaseUrl="http://ollama:11434" \
  localinference:latest
```

### Docker Compose

Create `docker-compose.yml`:

```yaml
version: "3.8"

services:
  api:
    build:
      context: .
      dockerfile: Dockerfile
    container_name: localinference-api
    restart: unless-stopped
    ports:
      - "5000:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__DefaultConnection=Host=postgres;Database=LocalInference;Username=postgres;Password=${POSTGRES_PASSWORD}
      - Inference__Ollama__BaseUrl=http://ollama:11434
      - Inference__OpenRouter__ApiKey=${OPENROUTER_API_KEY}
    depends_on:
      postgres:
        condition: service_healthy
    networks:
      - localinference
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 40s

  postgres:
    image: pgvector/pgvector:pg16
    container_name: localinference-db
    restart: unless-stopped
    environment:
      - POSTGRES_DB=LocalInference
      - POSTGRES_USER=postgres
      - POSTGRES_PASSWORD=${POSTGRES_PASSWORD}
    volumes:
      - postgres_data:/var/lib/postgresql/data
    ports:
      - "5432:5432"
    networks:
      - localinference
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U postgres"]
      interval: 10s
      timeout: 5s
      retries: 5

  ollama:
    image: ollama/ollama:latest
    container_name: localinference-ollama
    restart: unless-stopped
    volumes:
      - ollama_data:/root/.ollama
    ports:
      - "11434:11434"
    networks:
      - localinference
    deploy:
      resources:
        reservations:
          devices:
            - driver: nvidia
              count: 1
              capabilities: [gpu]

  nginx:
    image: nginx:alpine
    container_name: localinference-nginx
    restart: unless-stopped
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - ./nginx.conf:/etc/nginx/nginx.conf:ro
      - ./ssl:/etc/nginx/ssl:ro
    depends_on:
      - api
    networks:
      - localinference

volumes:
  postgres_data:
  ollama_data:

networks:
  localinference:
    driver: bridge
```

Create `.env` file:

```bash
POSTGRES_PASSWORD=your_secure_password_here
OPENROUTER_API_KEY=your_openrouter_key_here
```

Deploy:

```bash
docker-compose up -d
```

## Kubernetes Deployment

### Namespace

```yaml
apiVersion: v1
kind: Namespace
metadata:
  name: localinference
```

### ConfigMap

```yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: localinference-config
  namespace: localinference
data:
  ASPNETCORE_ENVIRONMENT: "Production"
  Inference__Ollama__BaseUrl: "http://ollama:11434"
```

### Secrets

```yaml
apiVersion: v1
kind: Secret
metadata:
  name: localinference-secrets
  namespace: localinference
type: Opaque
stringData:
  ConnectionStrings__DefaultConnection: "Host=postgres;Database=LocalInference;Username=postgres;Password=secret"
  Inference__OpenRouter__ApiKey: ""
```

### PostgreSQL StatefulSet

```yaml
apiVersion: apps/v1
kind: StatefulSet
metadata:
  name: postgres
  namespace: localinference
spec:
  serviceName: postgres
  replicas: 1
  selector:
    matchLabels:
      app: postgres
  template:
    metadata:
      labels:
        app: postgres
    spec:
      containers:
        - name: postgres
          image: pgvector/pgvector:pg16
          ports:
            - containerPort: 5432
          env:
            - name: POSTGRES_DB
              value: "LocalInference"
            - name: POSTGRES_USER
              value: "postgres"
            - name: POSTGRES_PASSWORD
              valueFrom:
                secretKeyRef:
                  name: localinference-secrets
                  key: postgres-password
          volumeMounts:
            - name: postgres-storage
              mountPath: /var/lib/postgresql/data
  volumeClaimTemplates:
    - metadata:
        name: postgres-storage
      spec:
        accessModes: ["ReadWriteOnce"]
        resources:
          requests:
            storage: 50Gi
---
apiVersion: v1
kind: Service
metadata:
  name: postgres
  namespace: localinference
spec:
  selector:
    app: postgres
  ports:
    - port: 5432
      targetPort: 5432
```

### API Deployment

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: localinference-api
  namespace: localinference
spec:
  replicas: 2
  selector:
    matchLabels:
      app: localinference-api
  template:
    metadata:
      labels:
        app: localinference-api
    spec:
      containers:
        - name: api
          image: localinference:latest
          ports:
            - containerPort: 8080
          envFrom:
            - configMapRef:
                name: localinference-config
            - secretRef:
                name: localinference-secrets
          resources:
            requests:
              memory: "512Mi"
              cpu: "500m"
            limits:
              memory: "2Gi"
              cpu: "2000m"
          livenessProbe:
            httpGet:
              path: /health
              port: 8080
            initialDelaySeconds: 30
            periodSeconds: 10
          readinessProbe:
            httpGet:
              path: /health
              port: 8080
            initialDelaySeconds: 5
            periodSeconds: 5
---
apiVersion: v1
kind: Service
metadata:
  name: localinference-api
  namespace: localinference
spec:
  selector:
    app: localinference-api
  ports:
    - port: 80
      targetPort: 8080
  type: ClusterIP
```

### Ingress

```yaml
apiVersion: networking.k8s.io/v1
kind: Ingress
metadata:
  name: localinference-ingress
  namespace: localinference
  annotations:
    nginx.ingress.kubernetes.io/ssl-redirect: "true"
    cert-manager.io/cluster-issuer: "letsencrypt-prod"
spec:
  tls:
    - hosts:
        - api.yourdomain.com
      secretName: localinference-tls
  rules:
    - host: api.yourdomain.com
      http:
        paths:
          - path: /
            pathType: Prefix
            backend:
              service:
                name: localinference-api
                port:
                  number: 80
```

Deploy:

```bash
kubectl apply -f k8s/
```

## Cloud Deployment

### AWS ECS

Create `ecs-task-definition.json`:

```json
{
  "family": "localinference",
  "networkMode": "awsvpc",
  "requiresCompatibilities": ["FARGATE"],
  "cpu": "1024",
  "memory": "2048",
  "executionRoleArn": "arn:aws:iam::ACCOUNT:role/ecsTaskExecutionRole",
  "containerDefinitions": [
    {
      "name": "api",
      "image": "your-registry/localinference:latest",
      "portMappings": [
        {
          "containerPort": 8080,
          "protocol": "tcp"
        }
      ],
      "environment": [
        {
          "name": "ASPNETCORE_ENVIRONMENT",
          "value": "Production"
        }
      ],
      "secrets": [
        {
          "name": "ConnectionStrings__DefaultConnection",
          "valueFrom": "arn:aws:secretsmanager:region:ACCOUNT:secret:localinference-db"
        }
      ],
      "logConfiguration": {
        "logDriver": "awslogs",
        "options": {
          "awslogs-group": "/ecs/localinference",
          "awslogs-region": "us-east-1",
          "awslogs-stream-prefix": "api"
        }
      },
      "healthCheck": {
        "command": [
          "CMD-SHELL",
          "curl -f http://localhost:8080/health || exit 1"
        ],
        "interval": 30,
        "timeout": 5,
        "retries": 3
      }
    }
  ]
}
```

Deploy:

```bash
aws ecs register-task-definition --cli-input-json file://ecs-task-definition.json
aws ecs create-service \
  --cluster localinference \
  --service-name api \
  --task-definition localinference \
  --desired-count 2 \
  --launch-type FARGATE \
  --network-configuration "awsvpcConfiguration={subnets=[subnet-xxx],securityGroups=[sg-xxx],assignPublicIp=ENABLED}"
```

### Azure Container Instances

```bash
az container create \
  --resource-group myResourceGroup \
  --name localinference \
  --image localinference:latest \
  --cpu 2 \
  --memory 4 \
  --ports 8080 \
  --environment-variables ASPNETCORE_ENVIRONMENT=Production \
  --secrets ConnectionStrings__DefaultConnection=secret_value \
  --secrets-secure-connection-strings
```

### Google Cloud Run

```bash
gcloud run deploy localinference \
  --image gcr.io/PROJECT/localinference:latest \
  --platform managed \
  --region us-central1 \
  --allow-unauthenticated \
  --set-env-vars ASPNETCORE_ENVIRONMENT=Production \
  --set-secrets ConnectionStrings__DefaultConnection=db-connection:latest
```

## Reverse Proxy Setup

### Nginx

Create `nginx.conf`:

```nginx
events {
    worker_connections 1024;
}

http {
    upstream api {
        server api:8080;
    }

    server {
        listen 80;
        server_name api.yourdomain.com;

        # Redirect HTTP to HTTPS
        return 301 https://$server_name$request_uri;
    }

    server {
        listen 443 ssl http2;
        server_name api.yourdomain.com;

        ssl_certificate /etc/nginx/ssl/cert.pem;
        ssl_certificate_key /etc/nginx/ssl/key.pem;
        ssl_protocols TLSv1.2 TLSv1.3;
        ssl_ciphers HIGH:!aNULL:!MD5;
        ssl_prefer_server_ciphers on;

        # Security headers
        add_header X-Frame-Options "SAMEORIGIN" always;
        add_header X-Content-Type-Options "nosniff" always;
        add_header X-XSS-Protection "1; mode=block" always;
        add_header Referrer-Policy "strict-origin-when-cross-origin" always;

        # Rate limiting
        limit_req_zone $binary_remote_addr zone=api:10m rate=10r/s;
        limit_req zone=api burst=20 nodelay;

        location / {
            proxy_pass http://api;
            proxy_http_version 1.1;
            proxy_set_header Upgrade $http_upgrade;
            proxy_set_header Connection "upgrade";
            proxy_set_header Host $host;
            proxy_set_header X-Real-IP $remote_addr;
            proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
            proxy_set_header X-Forwarded-Proto $scheme;

            # Timeouts
            proxy_connect_timeout 60s;
            proxy_send_timeout 60s;
            proxy_read_timeout 300s;

            # Buffering
            proxy_buffering off;
            proxy_cache off;
        }

        location /health {
            proxy_pass http://api/health;
            access_log off;
        }
    }
}
```

### Traefik

```yaml
# docker-compose.yml
version: "3.8"

services:
  traefik:
    image: traefik:v3.0
    command:
      - "--api.insecure=true"
      - "--providers.docker=true"
      - "--entrypoints.web.address=:80"
      - "--entrypoints.websecure.address=:443"
      - "--certificatesresolvers.letsencrypt.acme.tlschallenge=true"
      - "--certificatesresolvers.letsencrypt.acme.email=admin@yourdomain.com"
      - "--certificatesresolvers.letsencrypt.acme.storage=/letsencrypt/acme.json"
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - /var/run/docker.sock:/var/run/docker.sock:ro
      - ./letsencrypt:/letsencrypt

  api:
    image: localinference:latest
    labels:
      - "traefik.enable=true"
      - "traefik.http.routers.api.rule=Host(`api.yourdomain.com`)"
      - "traefik.http.routers.api.entrypoints=websecure"
      - "traefik.http.routers.api.tls.certresolver=letsencrypt"
      - "traefik.http.services.api.loadbalancer.server.port=8080"
```

## SSL/TLS Configuration

### Let's Encrypt with Certbot

```bash
# Install Certbot
sudo apt install certbot python3-certbot-nginx

# Obtain certificate
sudo certbot --nginx -d api.yourdomain.com

# Auto-renewal
sudo systemctl enable certbot.timer
```

### Self-Signed Certificates (Development)

```bash
# Generate private key
openssl genrsa -out key.pem 2048

# Generate certificate
openssl req -new -x509 -key key.pem -out cert.pem -days 365 \
  -subj "/C=US/ST=State/L=City/O=Organization/CN=api.yourdomain.com"
```

## Monitoring

### Prometheus Metrics

Add Prometheus exporter:

```csharp
// Program.cs
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics.AddAspNetCoreInstrumentation();
        metrics.AddHttpClientInstrumentation();
        metrics.AddPrometheusExporter();
    });

app.MapPrometheusScrapingEndpoint();
```

### Grafana Dashboard

Create dashboard JSON with panels for:

- Request rate
- Response time
- Error rate
- Token usage
- Session count
- Database connections

### Health Checks

```csharp
// Program.cs
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>()
    .AddCheck<OllamaHealthCheck>("ollama");

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = HealthCheckResponseWriter
});
```

### Logging

```csharp
// Structured logging with Serilog
builder.Host.UseSerilog((context, config) =>
{
    config
        .WriteTo.Console()
        .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
        .WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri("http://elasticsearch:9200")))
        .Enrich.WithProperty("Application", "LocalInference");
});
```

## Backup and Recovery

### Database Backup

```bash
# Automated backup script
#!/bin/bash
BACKUP_DIR="/backups"
DATE=$(date +%Y%m%d_%H%M%S)
FILENAME="localinference_$DATE.sql"

# Create backup
pg_dump -h postgres -U postgres LocalInference > "$BACKUP_DIR/$FILENAME"

# Compress
gzip "$BACKUP_DIR/$FILENAME"

# Upload to S3 (optional)
aws s3 cp "$BACKUP_DIR/$FILENAME.gz" s3://your-backup-bucket/

# Keep only last 7 days
find $BACKUP_DIR -name "localinference_*.sql.gz" -mtime +7 -delete
```

### Restore

```bash
# Download from S3 (if needed)
aws s3 cp s3://your-backup-bucket/localinference_20240115_120000.sql.gz .

# Decompress
gunzip localinference_20240115_120000.sql.gz

# Restore
psql -h postgres -U postgres -d LocalInference -f localinference_20240115_120000.sql
```

## Entity State Management in Production

The API uses **optimistic concurrency control** via Entity Framework Core. This is critical for production stability.

### Configuration

Ensure your `appsettings.Production.json` includes:

```json
{
  "Logging": {
    "LogLevel": {
      "Microsoft.EntityFrameworkCore.Database.Command": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning",
      "Microsoft.EntityFrameworkCore.Database.Transaction": "Warning"
    }
  }
}
```

### Key Practices

1. **Always reload entities before modification:**
   - Fresh loads ensure state matches database
   - Prevents concurrency exceptions
   - Particularly important after long operations (inference)

2. **Monitor for concurrency exceptions:**

   ```regex
   DbUpdateConcurrencyException|expected to affect.*but actually affected
   ```

   These indicate entity state mismatches and should be logged carefully.

3. **Database migrations are critical:**
   - Always run `dotnet ef database update` before deployment
   - Schema changes must be migrated before code changes
   - Use connection pooling (PgBouncer recommended)

4. **Inference service state management:**
   - Sessions are reloaded fresh before saving messages
   - This prevents state conflicts after long Ollama API calls
   - Pattern is implemented in `InferenceService.SaveMessagesAsync()`

### Troubleshooting Production Issues

**If you see concurrency exceptions:**

1. Verify migrations are applied: `SELECT * FROM __EFMigrationsHistory;`
2. Check for concurrent access: Look for multiple processes/containers writing
3. Verify all API instances use same database
4. Enable detailed logging temporarily to diagnose

**Connection pool issues:**

```bash
# Check PgBouncer stats
psql -U pgbouncer -h localhost -p 6432 -d pgbouncer -c "SHOW POOLS;"
```

## Scaling

### Horizontal Scaling

```yaml
# Kubernetes HPA
apiVersion: autoscaling/v2
kind: HorizontalPodAutoscaler
metadata:
  name: localinference-api
  namespace: localinference
spec:
  scaleTargetRef:
    apiVersion: apps/v1
    kind: Deployment
    name: localinference-api
  minReplicas: 2
  maxReplicas: 10
  metrics:
    - type: Resource
      resource:
        name: cpu
        target:
          type: Utilization
          averageUtilization: 70
    - type: Resource
      resource:
        name: memory
        target:
          type: Utilization
          averageUtilization: 80
```

### Database Scaling

- **Read Replicas:** For read-heavy workloads
- **Connection Pooling:** Use PgBouncer
- **Partitioning:** Partition large tables by date

## Security Hardening

### Container Security

```dockerfile
# Use non-root user
RUN adduser --disabled-password --gecos "" appuser
USER appuser

# Read-only filesystem
read_only: true

# Drop capabilities
cap_drop:
  - ALL
cap_add:
  - NET_BIND_SERVICE
```

### Network Security

```yaml
# Kubernetes NetworkPolicy
apiVersion: networking.k8s.io/v1
kind: NetworkPolicy
metadata:
  name: localinference-api
  namespace: localinference
spec:
  podSelector:
    matchLabels:
      app: localinference-api
  policyTypes:
    - Ingress
    - Egress
  ingress:
    - from:
        - namespaceSelector:
            matchLabels:
              name: ingress-nginx
      ports:
        - protocol: TCP
          port: 8080
  egress:
    - to:
        - podSelector:
            matchLabels:
              app: postgres
      ports:
        - protocol: TCP
          port: 5432
```

### Secrets Management

```bash
# HashiCorp Vault
vault kv put secret/localinference \
  ConnectionStrings__DefaultConnection="..." \
  Inference__OpenRouter__ApiKey="..."

# Kubernetes External Secrets
apiVersion: external-secrets.io/v1beta1
kind: ExternalSecret
metadata:
  name: localinference-secrets
spec:
  refreshInterval: 1h
  secretStoreRef:
    kind: ClusterSecretStore
    name: vault
  target:
    name: localinference-secrets
  data:
  - secretKey: ConnectionStrings__DefaultConnection
    remoteRef:
      key: secret/localinference
      property: db-connection
```

### API Security

```csharp
// Rate limiting
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("fixed", opt =>
    {
        opt.PermitLimit = 100;
        opt.Window = TimeSpan.FromMinutes(1);
    });
});

app.UseRateLimiter();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("Production", policy =>
    {
        policy.WithOrigins("https://yourdomain.com")
              .WithMethods("GET", "POST")
              .WithHeaders("Authorization", "Content-Type");
    });
});
```
