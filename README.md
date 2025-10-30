# Plan E-commerce Papelería

**Fecha:** 30 de Octubre, 2025  
**Versión:** 1.0.0

---

## Índice
1. [Contexto Actual](#contexto-actual)
2. [Arquitectura Propuesta](#arquitectura-propuesta)
3. [Infraestructura](#infraestructura)
4. [Contratos de API](#contratos-de-api)
5. [Base de Datos](#base-de-datos)
6. [Autenticación](#autenticación)
7. [Roadmap](#roadmap)

---

## Contexto Actual

### Sistema POS Existente
- **Stack:** .NET 8.0
- **Base de datos:** PostgreSQL (schema: `public`)
- **Storage:** MinIO (imágenes de productos)
- **Infraestructura:** k3s cluster (3 nodos ARM64)
- **CI/CD:** GitHub Actions + ArgoCD
- **Volumen:** ~50-100 ventas diarias

### Tablas principales en uso:
- `public.productos`
- `public.inventario`
- `public.ventas`
- `public.imagenes_productos`

---

## Arquitectura Propuesta

### Diagrama de Componentes

```
┌─────────────────────────────────────────────────────┐
│                    FRONTEND                         │
├─────────────────────────────────────────────────────┤
│  POS (Blazor/Razor)          E-commerce (Angular)   │
│         │                              │            │
└─────────┼──────────────────────────────┼────────────┘
          │                              │
          ▼                              ▼
┌──────────────────┐          ┌──────────────────────┐
│   POS API        │◄─────────│  E-commerce API      │
│   (dotnet)       │  consulta│  (dotnet)            │
│                  │  stock   │                      │
│  Endpoints:      │          │  Endpoints:          │
│  - GET /productos│          │  - Auth (Google/FB)  │
│  - GET /stock    │          │  - Carritos          │
│  - POST /venta   │◄─────────│  - Checkout          │
│                  │  registra│  - Usuarios          │
│                  │  venta   │                      │
└────────┬─────────┘          └──────────┬───────────┘
         │                               │
         ▼                               ▼
┌──────────────────┐          ┌──────────────────────┐
│  DB Papelería    │          │  DB E-commerce       │
│  (Postgres)      │          │  (Postgres)          │
│                  │          │                      │
│  - productos     │          │  - usuarios          │
│  - inventario    │          │  - carritos          │
│  - ventas        │          │  - ordenes           │
└──────────────────┘          └──────────────────────┘
         │                               │
         └───────────┬───────────────────┘
                     ▼
              ┌─────────────┐
              │   MinIO     │
              │  (Imágenes) │
              └─────────────┘
```

### Principios de Diseño

1. **Separación de responsabilidades**
   - POS API: Maneja inventario, ventas físicas, productos
   - E-commerce API: Maneja usuarios web, carritos, órdenes online

2. **Bases de datos aisladas**
   - DB Papelería: Solo POS API escribe
   - DB E-commerce: Solo E-commerce API escribe
   - Comunicación vía APIs REST

3. **Storage compartido**
   - MinIO sirve imágenes a ambas aplicaciones
   - URLs públicas para productos

4. **Escalabilidad independiente**
   - Cada API puede escalar sin afectar la otra
   - Despliegues independientes

---

## Infraestructura

### Cluster k3s Actual
```
NAME       ROLES                  INTERNAL-IP   OS-IMAGE
chaac      worker                 192.168.0.5   Debian 12 (ARM64)
ixchel     worker                 192.168.0.3   Debian 12 (ARM64)
kukulkan   control-plane,master   192.168.0.4   Debian 12 (ARM64)
```

**Recursos totales:**
- RAM: 32 GiB (16 GiB + 8 GiB + 8 GiB)
- Storage: 7.8 TB (Longhorn)

### Namespaces

- `papeleria` - POS y componentes actuales (producción)
- `dev-papeleria` - Ambiente de desarrollo (futuro)
- `ecommerce` - Shopping cart y API e-commerce (nuevo)

### Componentes por desplegar

#### E-commerce API
- **Deployment:** ecommerce-api
- **Service:** ecommerce-api-service
- **Ingress:** ecommerce.xplaya.com
- **Secrets:** Sealed Secrets para OAuth keys

#### Base de datos E-commerce
- **Opción 1:** Nueva instancia PostgreSQL
- **Opción 2:** Nueva database en PostgreSQL existente
- **Nombre:** `ecommerce_db`

---

## Contratos de API

### POS API (Endpoints públicos para E-commerce)

#### 1. GET /api/productos
Lista productos disponibles para venta online.

**Query Parameters:**
- `disponibles` (bool): Solo productos con stock > 0
- `categoria` (string): Filtrar por categoría
- `page` (int): Número de página (default: 1)
- `limit` (int): Items por página (default: 20)

**Response 200:**
```json
{
  "productos": [
    {
      "id": 123,
      "nombre": "Cuaderno profesional",
      "descripcion": "100 hojas cuadriculado",
      "precio": 45.50,
      "stock": 25,
      "categoria": "papeleria",
      "imagen_url": "https://minio.xplaya.com/productos/cuaderno-123.jpg"
    }
  ],
  "total": 150,
  "page": 1,
  "limit": 20
}
```

#### 2. GET /api/productos/{id}
Detalle completo de un producto.

**Path Parameters:**
- `id` (int): ID del producto

**Response 200:**
```json
{
  "id": 123,
  "nombre": "Cuaderno profesional",
  "descripcion": "100 hojas cuadriculado marca X",
  "precio": 45.50,
  "stock": 25,
  "categoria": "papeleria",
  "imagen_url": "https://minio.xplaya.com/productos/cuaderno-123.jpg",
  "imagenes_adicionales": [
    "https://minio.xplaya.com/productos/cuaderno-123-2.jpg",
    "https://minio.xplaya.com/productos/cuaderno-123-3.jpg"
  ],
  "videos": [
    "https://www.tiktok.com/@tupapeleria/video/1234567890",
    "https://www.tiktok.com/@tupapeleria/video/9876543210"
  ]
}
```

**Response 404:**
```json
{
  "error": "Producto no encontrado"
}
```

#### 3. POST /api/ventas
Registra una venta proveniente de e-commerce.

**Headers:**
- `Authorization: Bearer {api-key}`

**Request Body:**
```json
{
  "orden_id": "ecom-12345",
  "items": [
    {
      "producto_id": 123,
      "cantidad": 2,
      "precio_unitario": 45.50
    }
  ],
  "total": 91.00,
  "metodo_pago": "tarjeta",
  "cliente": {
    "email": "cliente@email.com",
    "nombre": "Juan Pérez"
  }
}
```

**Response 201:**
```json
{
  "venta_id": 5678,
  "status": "confirmada",
  "fecha": "2025-10-30T10:30:00Z",
  "items_procesados": 1
}
```

**Response 400:**
```json
{
  "error": "Stock insuficiente",
  "producto_id": 123,
  "stock_disponible": 1,
  "cantidad_solicitada": 2
}
```

---

### E-commerce API (Endpoints para Angular Frontend)

#### 1. POST /api/auth/login
Autenticación con OAuth (Google/Facebook).

**Request Body:**
```json
{
  "provider": "google",
  "token": "google-oauth-token-here"
}
```

**Response 200:**
```json
{
  "access_token": "jwt-token-here",
  "refresh_token": "refresh-token-here",
  "expires_in": 3600,
  "user": {
    "id": "user-uuid-123",
    "email": "usuario@gmail.com",
    "nombre": "Juan Pérez",
    "avatar_url": "https://..."
  }
}
```

#### 2. GET /api/carrito
Obtiene el carrito del usuario actual.

**Headers:**
- `Authorization: Bearer {jwt-token}`

**Response 200:**
```json
{
  "id": "cart-uuid-456",
  "items": [
    {
      "id": "item-uuid-1",
      "producto_id": 123,
      "nombre": "Cuaderno profesional",
      "cantidad": 2,
      "precio_unitario": 45.50,
      "subtotal": 91.00,
      "imagen_url": "https://minio.xplaya.com/productos/cuaderno-123.jpg",
      "stock_disponible": 25
    }
  ],
  "total": 91.00,
  "items_count": 1
}
```

#### 3. POST /api/carrito/items
Agregar producto al carrito.

**Headers:**
- `Authorization: Bearer {jwt-token}`

**Request Body:**
```json
{
  "producto_id": 123,
  "cantidad": 2
}
```

**Response 201:**
```json
{
  "item_id": "item-uuid-1",
  "producto_id": 123,
  "cantidad": 2,
  "subtotal": 91.00
}
```

#### 4. PATCH /api/carrito/items/{item_id}
Actualizar cantidad de un item.

**Headers:**
- `Authorization: Bearer {jwt-token}`

**Request Body:**
```json
{
  "cantidad": 3
}
```

#### 5. DELETE /api/carrito/items/{item_id}
Remover producto del carrito.

**Headers:**
- `Authorization: Bearer {jwt-token}`

**Response 204:** No content

#### 6. POST /api/checkout
Procesar pago y completar orden.

**Headers:**
- `Authorization: Bearer {jwt-token}`

**Request Body:**
```json
{
  "metodo_pago": "tarjeta",
  "datos_pago": {
    "token": "stripe-payment-token"
  },
  "direccion_envio": {
    "calle": "Av. Principal 123",
    "colonia": "Centro",
    "ciudad": "Cancún",
    "estado": "Quintana Roo",
    "cp": "77500"
  }
}
```

**Response 201:**
```json
{
  "orden_id": "ecom-12345",
  "status": "procesando",
  "total": 91.00,
  "fecha": "2025-10-30T10:30:00Z"
}
```

#### 7. GET /api/ordenes
Historial de órdenes del usuario.

**Headers:**
- `Authorization: Bearer {jwt-token}`

**Response 200:**
```json
{
  "ordenes": [
    {
      "id": "ecom-12345",
      "fecha": "2025-10-30T10:30:00Z",
      "total": 91.00,
      "status": "entregada",
      "items_count": 2
    }
  ],
  "total": 15,
  "page": 1
}
```

---

## Base de Datos

### Schema E-commerce (Nueva DB)

#### Tabla: usuarios
```sql
CREATE TABLE ecommerce.usuarios (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    email VARCHAR(255) UNIQUE NOT NULL,
    nombre VARCHAR(255),
    avatar_url TEXT,
    provider VARCHAR(50), -- 'google', 'facebook'
    provider_id VARCHAR(255),
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW()
);
```

#### Tabla: carritos
```sql
CREATE TABLE ecommerce.carritos (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    usuario_id UUID REFERENCES ecommerce.usuarios(id),
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW()
);
```

#### Tabla: items_carrito
```sql
CREATE TABLE ecommerce.items_carrito (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    carrito_id UUID REFERENCES ecommerce.carritos(id) ON DELETE CASCADE,
    producto_id INTEGER NOT NULL, -- FK a public.productos (otra DB)
    cantidad INTEGER NOT NULL CHECK (cantidad > 0),
    precio_unitario DECIMAL(10,2) NOT NULL,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW(),
    UNIQUE(carrito_id, producto_id)
);
```

#### Tabla: ordenes
```sql
CREATE TABLE ecommerce.ordenes (
    id VARCHAR(50) PRIMARY KEY, -- formato: ecom-12345
    usuario_id UUID REFERENCES ecommerce.usuarios(id),
    total DECIMAL(10,2) NOT NULL,
    status VARCHAR(50) NOT NULL, -- 'procesando', 'confirmada', 'enviada', 'entregada', 'cancelada'
    metodo_pago VARCHAR(50),
    venta_id INTEGER, -- FK a public.ventas del POS (otra DB)
    direccion_envio JSONB,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW()
);
```

#### Tabla: items_orden
```sql
CREATE TABLE ecommerce.items_orden (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    orden_id VARCHAR(50) REFERENCES ecommerce.ordenes(id),
    producto_id INTEGER NOT NULL,
    nombre_producto VARCHAR(255),
    cantidad INTEGER NOT NULL,
    precio_unitario DECIMAL(10,2) NOT NULL,
    subtotal DECIMAL(10,2) NOT NULL
);
```

### Modificación Schema Papelería (DB Existente)

#### Agregar columna videos a productos
```sql
ALTER TABLE public.productos 
ADD COLUMN IF NOT EXISTS videos TEXT[];

COMMENT ON COLUMN public.productos.videos IS 'URLs de videos en TikTok del producto';
```

---

## Autenticación

### Estrategia: OAuth 2.0 + JWT

#### Providers soportados:
1. **Google OAuth 2.0**
   - Más común en la región
   - Fácil configuración
   
2. **Facebook Login**
   - Segunda opción popular
   - Instagram requiere Facebook Business Account

#### Flujo de autenticación:

```
1. Usuario hace clic en "Login con Google"
2. Angular redirige a Google OAuth
3. Google devuelve token de autorización
4. Angular envía token a E-commerce API
5. API valida token con Google
6. API crea/busca usuario en DB
7. API genera JWT propio
8. Angular guarda JWT en memoria (NO localStorage por seguridad)
9. Angular usa JWT en headers para requests subsecuentes
```

#### Estructura del JWT:
```json
{
  "sub": "user-uuid-123",
  "email": "usuario@gmail.com",
  "nombre": "Juan Pérez",
  "iat": 1698677400,
  "exp": 1698681000
}
```

---

## Roadmap

### Fase 1: Fundamentos (Semanas 1-2)
- [x] Versionado semántico de containers
- [x] CHANGELOG y documentación
- [ ] Setup E-commerce API base (.NET 8)
- [ ] Crear base de datos `ecommerce_db`
- [ ] Implementar esquema de tablas

### Fase 2: POS API (Semanas 3-4)
- [ ] Endpoint GET /api/productos
- [ ] Endpoint GET /api/productos/{id}
- [ ] Endpoint POST /api/ventas
- [ ] Agregar columna `videos` a tabla productos
- [ ] API key para autenticación entre APIs
- [ ] Tests de integración

### Fase 3: E-commerce API - Auth (Semanas 5-6)
- [ ] Configurar Google OAuth
- [ ] Configurar Facebook Login
- [ ] Endpoint POST /api/auth/login
- [ ] Generación y validación de JWT
- [ ] Middleware de autenticación
- [ ] Tests de autenticación

### Fase 4: E-commerce API - Carrito (Semanas 7-8)
- [ ] Endpoints CRUD de carrito
- [ ] Validación de stock contra POS API
- [ ] Manejo de sesión de usuario
- [ ] Tests de carrito

### Fase 5: E-commerce API - Checkout (Semanas 9-10)
- [ ] Integración con procesador de pagos (Stripe/PayPal)
- [ ] Endpoint POST /api/checkout
- [ ] Llamada a POS API para registrar venta
- [ ] Manejo de webhooks de pago
- [ ] Tests de checkout

### Fase 6: Frontend Angular (Semanas 11-14)
- [ ] Setup proyecto Angular
- [ ] Componentes de catálogo de productos
- [ ] Componente de carrito
- [ ] Integración con OAuth (Google/Facebook)
- [ ] Flujo completo de checkout
- [ ] Diseño responsive

### Fase 7: Deployment (Semanas 15-16)
- [ ] Manifiestos k8s para E-commerce API
- [ ] Manifiestos k8s para Frontend Angular
- [ ] Sealed Secrets para OAuth keys
- [ ] ArgoCD Application para e-commerce
- [ ] Ingress y certificados SSL
- [ ] Monitoreo y logs

### Fase 8: Testing y Launch (Semanas 17-18)
- [ ] Pruebas end-to-end
- [ ] Pruebas de carga
- [ ] Ajustes de performance
- [ ] Documentación final
- [ ] 🚀 Launch en producción

---

## Mejoras Futuras (Post-Launch)

### Corto plazo (1-3 meses):
- [ ] Sistema de notificaciones por email
- [ ] Panel de administración para gestionar productos
- [ ] Reportes de ventas online vs físicas
- [ ] Sistema de cupones/descuentos

### Mediano plazo (3-6 meses):
- [ ] Programa de lealtad
- [ ] Reviews y ratings de productos
- [ ] Recomendaciones personalizadas
- [ ] App móvil nativa

### Largo plazo (6+ meses):
- [ ] Múltiples sucursales
- [ ] Sistema de envíos automatizado
- [ ] Integración con marketplaces (MercadoLibre, Amazon)
- [ ] Analytics avanzado

---

## Notas Técnicas

### Seguridad
- APIs con HTTPS únicamente
- Rate limiting en todos los endpoints públicos
- Validación de input en todos los endpoints
- Secrets en Sealed Secrets (nunca en código)
- JWT con expiración corta (1 hora)

### Performance
- Cache de productos en memoria (5 minutos TTL)
- Paginación obligatoria en listados
- Índices en DB para queries frecuentes
- Compresión de imágenes en MinIO

### Monitoreo
- Logs estructurados (JSON)
- Métricas de Prometheus
- Dashboards en Grafana
- Alertas para errores críticos

---

## Recursos de Aprendizaje

- **Conventional Commits:** https://www.conventionalcommits.org/es/
- **Versionado Semántico:** https://semver.org/lang/es/
- **Keep a Changelog:** https://keepachangelog.com/es-ES/
- **OAuth 2.0:** https://oauth.net/2/
- **JWT Best Practices:** https://tools.ietf.org/html/rfc8725

---

**Documento vivo - Última actualización:** 2025-10-30