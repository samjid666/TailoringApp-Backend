# Tailoring Management System - Architecture Documentation

## Table of Contents
1. [System Overview](#system-overview)
2. [Technology Stack](#technology-stack)
3. [Backend Architecture](#backend-architecture)
4. [Frontend Architecture](#frontend-architecture)
5. [Database Design](#database-design)
6. [API Documentation](#api-documentation)
7. [Authentication & Authorization](#authentication--authorization)
8. [Deployment Architecture](#deployment-architecture)
9. [Security Implementation](#security-implementation)

---

## System Overview

### Purpose
A comprehensive tailoring management system that enables tailors to manage customer orders, track measurements, monitor order progress, and handle customer relationships efficiently.

### Key Features
- **Role-Based Access Control**: Admin and Customer roles with different permissions
- **Order Management**: Create, view, and track tailoring orders with detailed measurements
- **Customer Portal**: Customers can view their orders and track progress
- **Admin Dashboard**: Complete order management with pagination, sorting, and filtering
- **Measurement Tracking**: Store and manage 7 different body measurements
- **Payment Tracking**: Track advance payments and balance amounts
- **Priority Management**: High, Medium, Low priority orders
- **Status Tracking**: 9-stage order lifecycle (Pending → Delivered/Cancelled)

---

## Technology Stack

### Backend
- **Framework**: ASP.NET Core 9.0 Web API
- **Language**: C# 12
- **ORM**: Entity Framework Core 9.0
- **Database**: SQL Server LocalDB (Development) / SQL Server (Production)
- **Architecture**: Clean Architecture (Onion Architecture)
- **Authentication**: JWT Bearer Tokens
- **Password Hashing**: HMACSHA512 with Salt

### Frontend
- **Framework**: React 19
- **Language**: TypeScript 5.6
- **State Management**: Redux Toolkit with Redux Saga
- **Routing**: React Router v6
- **UI Framework**: React Bootstrap
- **HTTP Client**: Axios
- **Styling**: CSS3 with Gradients and Animations

### Development Tools
- **IDE**: Visual Studio Code
- **Version Control**: Git
- **Package Manager**: NuGet (Backend), npm (Frontend)
- **API Testing**: REST Client / Postman

---

## Backend Architecture

### Clean Architecture Layers

```
Tailoring/
├── Tailoring.API/              # Presentation Layer
│   ├── Controllers/            # API Endpoints
│   ├── Program.cs             # Application Entry Point
│   └── appsettings.json       # Configuration
│
├── Tailoring.Core/            # Domain Layer (Business Logic)
│   ├── Entities/              # Domain Models
│   ├── DTOs/                  # Data Transfer Objects
│   ├── Interfaces/            # Service Contracts
│   ├── Services/              # Business Logic Implementation
│   ├── Enums/                 # Enumerations
│   └── Common/                # Shared Domain Logic
│
├── Tailoring.Infrastructure/   # Infrastructure Layer
│   ├── Data/                  # Database Context
│   ├── Repositories/          # Data Access Implementation
│   └── Migrations/            # EF Core Migrations
│
└── Tailoring.Shared/          # Shared Utilities
    └── Common Classes
```

### Layer Responsibilities

#### 1. API Layer (Tailoring.API)
**Purpose**: Handle HTTP requests and responses, route management, and API documentation.

**Key Components**:
- **Controllers**:
  - `AuthController`: Handles login and registration
  - `OrdersController`: CRUD operations for orders with pagination
  - `CustomersController`: Customer management

**Implementation Details**:
```csharp
// Program.cs - Service Registration
builder.Services.AddControllers()
    .AddJsonOptions(options => {
        options.JsonSerializerOptions.ReferenceHandler = 
            System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

// JWT Configuration
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => {
        options.TokenValidationParameters = new TokenValidationParameters {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            // ... configuration
        };
    });
```

#### 2. Core Layer (Tailoring.Core)
**Purpose**: Contains all business logic and domain models.

**Entities**:
- **User**: Authentication and user management
  - Properties: Username, Email, PasswordHash, Role, CustomerId
  - Relations: One-to-One with Customer

- **Customer**: Customer information
  - Properties: FirstName, LastName, Email, Phone, Address
  - Relations: One-to-Many with Orders

- **Order**: Tailoring order details
  - Properties: OrderNumber, GarmentType, FabricType, Status, Priority, Dates, Amounts
  - Relations: Many-to-One with Customer, One-to-Many with Measurements

- **Measurement**: Body measurements
  - Properties: Neck, Shoulder, Chest, Waist, Hip, Inseam, ArmLength
  - Relations: Many-to-One with Order

- **OrderProgress**: Order status tracking
  - Properties: Stage, CompletedDate, Notes
  - Relations: Many-to-One with Order

**Services**:
- `AuthService`: JWT token generation, password hashing, user authentication
- `CustomerService`: Customer CRUD operations
- `OrderService`: Order management with business logic
- `MeasurementService`: Measurement data handling

**Key Implementation**:
```csharp
// Password Hashing with Salt
public string HashPassword(string password)
{
    using var hmac = new HMACSHA512();
    var salt = hmac.Key;
    var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
    return $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
}

// JWT Token Generation
public string GenerateJwtToken(User user)
{
    var claims = new List<Claim>
    {
        new Claim(ClaimTypes.Name, user.Username),
        new Claim(ClaimTypes.Email, user.Email),
        new Claim(ClaimTypes.Role, user.Role.ToString()),
        // ... additional claims
    };
    
    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSecret));
    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    
    var token = new JwtSecurityToken(
        issuer: _jwtIssuer,
        audience: _jwtAudience,
        claims: claims,
        expires: DateTime.Now.AddHours(24),
        signingCredentials: creds
    );
    
    return new JwtSecurityTokenHandler().WriteToken(token);
}
```

#### 3. Infrastructure Layer (Tailoring.Infrastructure)
**Purpose**: Data access and external service integration.

**Database Context**:
```csharp
public class ApplicationDbContext : DbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<Customer> Customers { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<Measurement> Measurements { get; set; }
    public DbSet<OrderProgress> OrderProgresses { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure relationships
        modelBuilder.Entity<Order>()
            .HasOne(o => o.Customer)
            .WithMany(c => c.Orders)
            .HasForeignKey(o => o.CustomerId);
            
        // Configure indexes for performance
        modelBuilder.Entity<Order>()
            .HasIndex(o => o.OrderNumber)
            .IsUnique();
    }
}
```

**Repository Pattern**:
- Generic repository for common CRUD operations
- Specific repositories for complex queries
- Unit of Work pattern for transaction management

**Data Seeding**:
```csharp
public static class DbSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        // Seed Admin User
        if (!context.Users.Any(u => u.Role == UserRole.Admin))
        {
            var admin = new User
            {
                Username = "admin",
                Email = "admin@tailoring.com",
                PasswordHash = HashPassword("admin123"),
                Role = UserRole.Admin
            };
            context.Users.Add(admin);
        }
        
        // Seed Customers (5 customers)
        // Seed Orders (6 sample orders)
        // Seed Measurements (3 measurement sets)
        
        await context.SaveChangesAsync();
    }
}
```

---

## Frontend Architecture

### Project Structure

```
Frontend/
├── public/
│   ├── index.html
│   └── manifest.json
│
├── src/
│   ├── components/           # Reusable UI Components
│   │   ├── common/
│   │   │   └── LoadingSpinner.tsx
│   │   └── orders/
│   │       ├── OrderCard.tsx
│   │       ├── OrderForm.tsx
│   │       └── OrderForm.css
│   │
│   ├── pages/                # Page Components
│   │   ├── Login.tsx
│   │   ├── Login.css
│   │   ├── Register.tsx
│   │   ├── Register.css
│   │   ├── Dashboard.tsx
│   │   ├── Dashboard.css
│   │   ├── CustomerPortal.tsx
│   │   └── CustomerPortal.css
│   │
│   ├── store/                # Redux State Management
│   │   ├── index.ts          # Store Configuration
│   │   ├── slices/
│   │   │   ├── authSlice.ts  # Authentication State
│   │   │   └── orderSlice.ts # Order State
│   │   ├── sagas/
│   │   │   └── orderSagas.ts # Side Effects
│   │   └── types/
│   │       └── index.ts      # Type Definitions
│   │
│   ├── services/
│   │   └── api.ts            # API Client Configuration
│   │
│   ├── App.tsx               # Root Component
│   ├── App.css
│   ├── index.tsx             # Application Entry
│   └── index.css
│
├── package.json
└── tsconfig.json
```

### State Management Architecture

#### Redux Store Structure
```typescript
// Store State Shape
{
  auth: {
    user: {
      username: string,
      email: string,
      role: 'Admin' | 'Customer',
      customerId?: number
    } | null,
    token: string | null,
    loading: boolean,
    error: string | null
  },
  orders: {
    items: Order[],
    currentOrder: Order | null,
    loading: boolean,
    error: string | null,
    pagination: {
      currentPage: number,
      totalPages: number,
      pageSize: number
    }
  }
}
```

#### Authentication Slice
```typescript
// authSlice.ts
export const login = createAsyncThunk<LoginResponse, LoginCredentials>(
  'auth/login',
  async (credentials, { rejectWithValue }) => {
    try {
      const response = await axios.post(`${API_BASE_URL}/auth/login`, credentials);
      // Store token in localStorage
      localStorage.setItem('token', response.data.token);
      localStorage.setItem('user', JSON.stringify(response.data));
      return response.data;
    } catch (error: any) {
      return rejectWithValue(error.response?.data?.message || 'Login failed');
    }
  }
);
```

### Routing Architecture

```typescript
// App.tsx - Protected Routes
<Routes>
  {/* Public Routes */}
  <Route path="/login" element={<Login />} />
  <Route path="/register" element={<Register />} />
  
  {/* Admin Routes - Role-Based Protection */}
  <Route
    path="/admin/dashboard"
    element={
      <ProtectedRoute requiredRole="Admin">
        <Dashboard />
      </ProtectedRoute>
    }
  />
  
  {/* Customer Routes - Role-Based Protection */}
  <Route
    path="/customer/portal"
    element={
      <ProtectedRoute requiredRole="Customer">
        <CustomerPortal />
      </ProtectedRoute>
    }
  />
  
  {/* Default Route */}
  <Route path="/" element={<Navigate to="/login" replace />} />
</Routes>
```

### Component Architecture

#### 1. Dashboard (Admin View)
**Features**:
- Stats cards showing Total Orders, Completed, In Progress
- Order sorting (Priority, Date, Due Date, Status)
- Pagination (9 orders per page)
- New Order modal with OrderForm
- Order cards with priority badges and status

**Implementation**:
```typescript
// Key State
const [orders, setOrders] = useState<Order[]>([]);
const [pageNumber, setPageNumber] = useState(1);
const [sortBy, setSortBy] = useState('Priority');
const [showOrderForm, setShowOrderForm] = useState(false);

// Fetch Orders with Pagination
useEffect(() => {
  fetchOrders();
}, [pageNumber, sortBy]);

const fetchOrders = async () => {
  const response = await axios.get('http://localhost:5167/api/orders', {
    params: { pageNumber, pageSize: 9, sortBy },
    headers: { Authorization: `Bearer ${token}` }
  });
  setOrders(response.data.items);
  setTotalPages(response.data.totalPages);
};
```

#### 2. Customer Portal
**Features**:
- View own orders only (filtered by customerId)
- Progress bars showing order completion
- Days remaining calculation
- Payment breakdown (Total, Paid, Balance)
- Status badges (Ready for Pickup, Stitching in Progress, etc.)

**Implementation**:
```typescript
// Filter Orders by Customer
useEffect(() => {
  if (user?.customerId) {
    fetchCustomerOrders(user.customerId);
  }
}, [user]);

const fetchCustomerOrders = async (customerId: number) => {
  const response = await axios.get(
    `http://localhost:5167/api/orders/customer/${customerId}`,
    { headers: { Authorization: `Bearer ${token}` } }
  );
  setOrders(response.data);
};
```

#### 3. OrderForm Component
**Features**:
- Customer selection dropdown
- Garment and fabric details
- Due date picker with validation
- Priority selection (High/Medium/Low)
- 7 measurement fields (Neck, Shoulder, Chest, Waist, Hip, Inseam, Arm Length)
- Special instructions textarea
- Form validation
- Success/error alerts

**Measurement Collection**:
```typescript
const measurements = {
  neck: parseFloat(formData.neck) || 0,
  shoulder: parseFloat(formData.shoulder) || 0,
  chest: parseFloat(formData.chest) || 0,
  waist: parseFloat(formData.waist) || 0,
  hip: parseFloat(formData.hip) || 0,
  inseam: parseFloat(formData.inseam) || 0,
  armLength: parseFloat(formData.armLength) || 0
};
```

### Design System

#### Color Palette
```css
:root {
  /* Primary Colors */
  --primary-purple: #667eea;
  --primary-dark: #764ba2;
  
  /* Success Colors */
  --success-green: #48bb78;
  --success-dark: #38a169;
  
  /* Warning Colors */
  --warning-orange: #f6ad55;
  --warning-dark: #ed8936;
  
  /* Danger Colors */
  --danger-red: #f56565;
  --danger-dark: #e53e3e;
  
  /* Neutral Colors */
  --gray-50: #f7fafc;
  --gray-100: #edf2f7;
  --gray-200: #e2e8f0;
  --gray-700: #2d3748;
  --gray-800: #1a202c;
}
```

#### Typography
- **Headings**: System font stack with bold weights (700-800)
- **Body**: 16px base, 1.5 line height
- **Labels**: Uppercase, 0.5-1px letter-spacing, semi-bold (600)

#### Animations
```css
/* Hover Effects */
.card:hover {
  transform: translateY(-5px);
  box-shadow: 0 12px 24px rgba(102, 126, 234, 0.3);
  transition: all 0.3s ease;
}

/* Focus States */
.form-control:focus {
  border-color: #667eea;
  box-shadow: 0 0 0 4px rgba(102, 126, 234, 0.15);
  transform: translateY(-1px);
}
```

---

## Database Design

### Entity Relationship Diagram

```
┌─────────────┐         ┌──────────────┐
│    User     │         │   Customer   │
├─────────────┤         ├──────────────┤
│ Id (PK)     │◄───────►│ Id (PK)      │
│ Username    │   1:1   │ FirstName    │
│ Email       │         │ LastName     │
│ PasswordHash│         │ Email        │
│ Role        │         │ Phone        │
│ CustomerId  │         │ Address      │
└─────────────┘         └──────┬───────┘
                               │ 1
                               │
                               │ *
                        ┌──────▼───────┐
                        │    Order     │
                        ├──────────────┤
                        │ Id (PK)      │
                        │ OrderNumber  │
                        │ CustomerId   │
                        │ GarmentType  │
                        │ FabricType   │
                        │ Status       │
                        │ Priority     │
                        │ OrderDate    │
                        │ DueDate      │
                        │ TotalAmount  │
                        │ AdvancePaid  │
                        └──────┬───────┘
                               │ 1
                    ┌──────────┼──────────┐
                    │ *        │ *        │ *
             ┌──────▼──────┐   │   ┌─────▼────────┐
             │ Measurement │   │   │OrderProgress │
             ├─────────────┤   │   ├──────────────┤
             │ Id (PK)     │   │   │ Id (PK)      │
             │ OrderId     │   │   │ OrderId      │
             │ Neck        │   │   │ Stage        │
             │ Shoulder    │   │   │ CompletedDate│
             │ Chest       │   │   │ Notes        │
             │ Waist       │   │   └──────────────┘
             │ Hip         │   │
             │ Inseam      │   │
             │ ArmLength   │   │
             └─────────────┘   │
```

### Table Schemas

#### Users Table
```sql
CREATE TABLE Users (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Username NVARCHAR(50) NOT NULL UNIQUE,
    Email NVARCHAR(100) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(500) NOT NULL,
    Role INT NOT NULL,  -- 1: Admin, 2: Customer
    CustomerId INT NULL,
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    FOREIGN KEY (CustomerId) REFERENCES Customers(Id)
);
```

#### Customers Table
```sql
CREATE TABLE Customers (
    Id INT PRIMARY KEY IDENTITY(1,1),
    FirstName NVARCHAR(100) NOT NULL,
    LastName NVARCHAR(100) NOT NULL,
    Email NVARCHAR(100) NOT NULL,
    Phone NVARCHAR(20),
    Address NVARCHAR(500),
    CreatedAt DATETIME2 DEFAULT GETDATE()
);
```

#### Orders Table
```sql
CREATE TABLE Orders (
    Id INT PRIMARY KEY IDENTITY(1,1),
    OrderNumber NVARCHAR(50) NOT NULL UNIQUE,
    CustomerId INT NOT NULL,
    GarmentType NVARCHAR(100) NOT NULL,
    FabricType NVARCHAR(100) NOT NULL,
    Status INT NOT NULL DEFAULT 1,
    Priority INT NOT NULL DEFAULT 2,
    OrderDate DATETIME2 NOT NULL,
    DueDate DATETIME2 NOT NULL,
    TotalAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
    AdvancePaid DECIMAL(18,2) NOT NULL DEFAULT 0,
    SpecialInstructions NVARCHAR(1000),
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    FOREIGN KEY (CustomerId) REFERENCES Customers(Id),
    INDEX IX_Orders_OrderNumber (OrderNumber),
    INDEX IX_Orders_CustomerId (CustomerId),
    INDEX IX_Orders_Status (Status),
    INDEX IX_Orders_Priority (Priority)
);
```

#### Measurements Table
```sql
CREATE TABLE Measurements (
    Id INT PRIMARY KEY IDENTITY(1,1),
    OrderId INT NOT NULL,
    Neck DECIMAL(5,2) DEFAULT 0,
    Shoulder DECIMAL(5,2) DEFAULT 0,
    Chest DECIMAL(5,2) DEFAULT 0,
    Waist DECIMAL(5,2) DEFAULT 0,
    Hip DECIMAL(5,2) DEFAULT 0,
    Inseam DECIMAL(5,2) DEFAULT 0,
    ArmLength DECIMAL(5,2) DEFAULT 0,
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    FOREIGN KEY (OrderId) REFERENCES Orders(Id) ON DELETE CASCADE
);
```

#### OrderProgress Table
```sql
CREATE TABLE OrderProgresses (
    Id INT PRIMARY KEY IDENTITY(1,1),
    OrderId INT NOT NULL,
    Stage NVARCHAR(100) NOT NULL,
    CompletedDate DATETIME2,
    Notes NVARCHAR(500),
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    FOREIGN KEY (OrderId) REFERENCES Orders(Id) ON DELETE CASCADE
);
```

### Enumerations

#### OrderStatus Enum
```csharp
public enum OrderStatus
{
    Pending = 1,
    MeasurementTaken = 2,
    Cutting = 3,
    Stitching = 4,
    Finishing = 5,
    QualityCheck = 6,
    ReadyForDelivery = 7,
    Delivered = 8,
    Cancelled = 9
}
```

#### UserRole Enum
```csharp
public enum UserRole
{
    Admin = 1,
    Customer = 2
}
```

---

## API Documentation

### Base URL
- Development: `http://localhost:5167/api`
- Production: `https://your-domain.com/api`

### Authentication Endpoints

#### POST /api/auth/login
**Description**: Authenticate user and receive JWT token

**Request**:
```json
{
  "username": "admin",
  "password": "admin123"
}
```

**Response** (200 OK):
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "username": "admin",
  "email": "admin@tailoring.com",
  "role": "Admin",
  "customerId": null
}
```

**Error Response** (401 Unauthorized):
```json
{
  "message": "Invalid username or password"
}
```

#### POST /api/auth/register
**Description**: Register new customer account

**Request**:
```json
{
  "username": "john_doe",
  "email": "john@example.com",
  "password": "SecurePass123",
  "firstName": "John",
  "lastName": "Doe",
  "phone": "+1234567890"
}
```

**Response** (201 Created):
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "username": "john_doe",
  "email": "john@example.com",
  "role": "Customer",
  "customerId": 10
}
```

### Order Endpoints

#### GET /api/orders
**Description**: Get paginated list of orders (Admin only)

**Authorization**: Bearer Token required

**Query Parameters**:
- `pageNumber` (int): Page number (default: 1)
- `pageSize` (int): Items per page (default: 10)
- `sortBy` (string): Sort field - Priority, Date, DueDate, Status

**Response** (200 OK):
```json
{
  "items": [
    {
      "id": 1,
      "orderNumber": "ORD-20251129-001",
      "customer": {
        "firstName": "John",
        "lastName": "Doe"
      },
      "garmentType": "Three-Piece Suit",
      "fabricType": "Wool Blend",
      "status": 4,
      "priority": 1,
      "orderDate": "2025-11-24T00:00:00",
      "dueDate": "2025-12-09T00:00:00",
      "totalAmount": 450.00,
      "advancePaid": 200.00
    }
  ],
  "totalCount": 6,
  "pageNumber": 1,
  "pageSize": 10,
  "totalPages": 1
}
```

#### GET /api/orders/{id}
**Description**: Get order details by ID

**Authorization**: Bearer Token required

**Response** (200 OK):
```json
{
  "id": 1,
  "orderNumber": "ORD-20251129-001",
  "customerId": 1,
  "customer": {
    "id": 1,
    "firstName": "John",
    "lastName": "Doe",
    "email": "john.doe@email.com",
    "phone": "555-0101"
  },
  "garmentType": "Three-Piece Suit",
  "fabricType": "Wool Blend",
  "status": 4,
  "priority": 1,
  "orderDate": "2025-11-24T00:00:00",
  "dueDate": "2025-12-09T00:00:00",
  "totalAmount": 450.00,
  "advancePaid": 200.00,
  "specialInstructions": "Extra padding on shoulders",
  "measurements": [
    {
      "neck": 15.5,
      "shoulder": 18.0,
      "chest": 40.0,
      "waist": 34.0,
      "hip": 38.0,
      "inseam": 32.0,
      "armLength": 25.0
    }
  ],
  "orderProgresses": []
}
```

#### POST /api/orders
**Description**: Create new order (Admin only)

**Authorization**: Bearer Token required

**Request**:
```json
{
  "customerId": 1,
  "garmentType": "Blazer",
  "fabricType": "Cotton",
  "dueDate": "2025-12-15",
  "priority": 2,
  "specialInstructions": "Client prefers slim fit",
  "measurements": {
    "neck": 16.0,
    "shoulder": 18.5,
    "chest": 42.0,
    "waist": 36.0,
    "hip": 40.0,
    "inseam": 34.0,
    "armLength": 26.0
  }
}
```

**Response** (201 Created):
```json
{
  "id": 7,
  "orderNumber": "ORD-20251129-007",
  "customerId": 1,
  "garmentType": "Blazer",
  "fabricType": "Cotton",
  "status": 1,
  "priority": 2,
  "orderDate": "2025-11-29T00:00:00",
  "dueDate": "2025-12-15T00:00:00",
  "totalAmount": 0.00,
  "advancePaid": 0.00
}
```

#### GET /api/orders/customer/{customerId}
**Description**: Get orders for specific customer

**Authorization**: Bearer Token required (Customer can only access own orders)

**Response** (200 OK):
```json
[
  {
    "id": 1,
    "orderNumber": "ORD-20251129-001",
    "garmentType": "Three-Piece Suit",
    "fabricType": "Wool Blend",
    "status": 4,
    "orderDate": "2025-11-24T00:00:00",
    "dueDate": "2025-12-09T00:00:00",
    "totalAmount": 450.00,
    "advancePaid": 200.00
  }
]
```

### Customer Endpoints

#### GET /api/customers
**Description**: Get all customers (Admin only)

**Authorization**: Bearer Token required

**Response** (200 OK):
```json
[
  {
    "id": 1,
    "firstName": "John",
    "lastName": "Doe",
    "email": "john.doe@email.com",
    "phone": "555-0101",
    "address": "123 Main St"
  }
]
```

---

## Authentication & Authorization

### JWT Token Structure

**Header**:
```json
{
  "alg": "HS256",
  "typ": "JWT"
}
```

**Payload**:
```json
{
  "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name": "admin",
  "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress": "admin@tailoring.com",
  "http://schemas.microsoft.com/ws/2008/06/identity/claims/role": "Admin",
  "UserId": "1",
  "CustomerId": null,
  "exp": 1732910967,
  "iss": "TailoringApp",
  "aud": "TailoringAppUsers"
}
```

### Password Security

**Hashing Algorithm**: HMACSHA512 with random salt

**Storage Format**: `{salt}:{hash}` (Base64 encoded)

**Example**:
```
Input: "admin123"
Output: "xK7j9pL2m...==:yH8k3nM6p...=="
         └─ Salt ─┘ └── Hash ──┘
```

**Implementation**:
```csharp
// Hash Password
using var hmac = new HMACSHA512();
var salt = hmac.Key;
var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
return $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";

// Verify Password
var parts = storedHash.Split(':');
var salt = Convert.FromBase64String(parts[0]);
var hash = Convert.FromBase64String(parts[1]);
using var hmac = new HMACSHA512(salt);
var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
return hash.SequenceEqual(computedHash);
```

### Role-Based Access Control

#### Frontend Protection
```typescript
// ProtectedRoute Component
const ProtectedRoute: React.FC<ProtectedRouteProps> = ({ children, requiredRole }) => {
  const token = localStorage.getItem('token');
  const userStr = localStorage.getItem('user');
  
  if (!token || !userStr) {
    return <Navigate to="/login" replace />;
  }

  if (requiredRole) {
    const user = JSON.parse(userStr);
    if (user.role !== requiredRole) {
      return <Navigate to="/login" replace />;
    }
  }

  return children;
};
```

#### Backend Protection
```csharp
// Controller Authorization
[Authorize(Roles = "Admin")]
[HttpGet]
public async Task<ActionResult<PagedResult<Order>>> GetOrders(
    [FromQuery] int pageNumber = 1,
    [FromQuery] int pageSize = 10,
    [FromQuery] string sortBy = "Priority")
{
    // Only admins can access
}

[Authorize]
[HttpGet("customer/{customerId}")]
public async Task<ActionResult<IEnumerable<Order>>> GetCustomerOrders(int customerId)
{
    // Verify user can only access own orders
    var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
    var user = await _context.Users.FindAsync(userId);
    
    if (user.Role != UserRole.Admin && user.CustomerId != customerId)
    {
        return Forbid();
    }
    
    // Return orders
}
```

---

## Deployment Architecture

### Development Environment

**Backend**:
```bash
# Navigate to API project
cd Backend/Tailoring.API

# Apply migrations
dotnet ef database update

# Run application
dotnet run
```
- URL: http://localhost:5167, https://localhost:7192
- Database: SQL Server LocalDB
- Connection String: `Server=(localdb)\\mssqllocaldb;Database=TailoringDB;Trusted_Connection=true`

**Frontend**:
```bash
# Navigate to frontend
cd Frontend

# Install dependencies
npm install

# Start development server
npm start
```
- URL: http://localhost:3001
- Hot reload enabled
- Proxy to backend API

### Production Deployment

#### Backend (Azure/IIS)
1. **Build Application**:
```bash
dotnet publish -c Release -o ./publish
```

2. **Configuration**:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=prod-server;Database=TailoringDB;User=sa;Password=***"
  },
  "Jwt": {
    "Secret": "{Strong-Production-Secret}",
    "Issuer": "TailoringApp",
    "Audience": "TailoringAppUsers",
    "ExpiryHours": 24
  }
}
```

3. **Environment Variables**:
- `ASPNETCORE_ENVIRONMENT=Production`
- `ASPNETCORE_URLS=http://+:80;https://+:443`

#### Frontend (Static Hosting)
1. **Build Application**:
```bash
npm run build
```

2. **Deploy to**:
- Azure Static Web Apps
- Netlify
- Vercel
- AWS S3 + CloudFront

3. **Environment Configuration**:
```typescript
// Create .env.production
REACT_APP_API_URL=https://api.yourdomain.com
```

### Docker Deployment

**Backend Dockerfile**:
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80 443

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["Tailoring.API/Tailoring.API.csproj", "Tailoring.API/"]
RUN dotnet restore "Tailoring.API/Tailoring.API.csproj"
COPY . .
WORKDIR "/src/Tailoring.API"
RUN dotnet build "Tailoring.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Tailoring.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Tailoring.API.dll"]
```

**Frontend Dockerfile**:
```dockerfile
FROM node:20-alpine AS build
WORKDIR /app
COPY package*.json ./
RUN npm ci
COPY . .
RUN npm run build

FROM nginx:alpine
COPY --from=build /app/build /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
```

**docker-compose.yml**:
```yaml
version: '3.8'

services:
  db:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      SA_PASSWORD: "YourStrong@Password"
      ACCEPT_EULA: "Y"
    ports:
      - "1433:1433"
    volumes:
      - sqldata:/var/opt/mssql

  backend:
    build:
      context: ./Backend
      dockerfile: Dockerfile
    ports:
      - "5167:80"
    environment:
      - ConnectionStrings__DefaultConnection=Server=db;Database=TailoringDB;User=sa;Password=YourStrong@Password
    depends_on:
      - db

  frontend:
    build:
      context: ./Frontend
      dockerfile: Dockerfile
    ports:
      - "3001:80"
    depends_on:
      - backend

volumes:
  sqldata:
```

---

## Security Implementation

### 1. Authentication Security
- JWT tokens with 24-hour expiration
- HMACSHA512 password hashing with random salt
- Secure token storage in localStorage
- Token validation on every API request

### 2. Authorization Security
- Role-based access control (RBAC)
- Customer data isolation (users can only access own data)
- Admin-only endpoints protected with `[Authorize(Roles = "Admin")]`

### 3. API Security
- CORS configuration for specific origins
- HTTPS enforcement in production
- Input validation using Data Annotations
- SQL injection prevention via parameterized queries (EF Core)

### 4. Frontend Security
- XSS prevention through React's built-in escaping
- CSRF protection via JWT tokens (stateless)
- Secure credential storage
- Protected routes with authentication checks

### 5. Database Security
- Encrypted connections
- Least privilege database user
- Password hashing (never store plain text)
- Cascading deletes for data integrity

### 6. Best Practices Implemented
- Environment-specific configurations
- Secrets management (never commit secrets)
- Error handling without exposing sensitive data
- Logging without sensitive information
- Regular security updates

---

## Performance Optimizations

### Backend
- Database indexing on frequently queried columns
- Pagination for large datasets
- Eager loading with `.Include()` to avoid N+1 queries
- Async/await patterns throughout
- JSON cycle handling to prevent circular references

### Frontend
- React.memo for expensive components
- useCallback and useMemo for optimization
- Lazy loading routes with React.lazy()
- Debouncing search and filter operations
- Redux for centralized state management

### Database
- Indexes on OrderNumber, CustomerId, Status, Priority
- Proper foreign key relationships
- Cascade delete for orphaned records
- DateTime2 for better precision and range

---

## Testing Strategy

### Backend Testing
- Unit tests for services and business logic
- Integration tests for API endpoints
- Repository pattern tests with in-memory database
- Authentication and authorization tests

### Frontend Testing
- Component unit tests with Jest and React Testing Library
- Redux store tests for actions and reducers
- Integration tests for user workflows
- E2E tests with Cypress (recommended)

---

## Future Enhancements

### Planned Features
1. **Order Editing**: Allow admins to update existing orders
2. **Order Deletion**: Soft delete with archive functionality
3. **Advanced Search**: Full-text search across orders
4. **Email Notifications**: Order status updates via email
5. **SMS Notifications**: Delivery reminders
6. **Payment Integration**: Stripe/PayPal integration
7. **Invoice Generation**: PDF invoice creation
8. **Reporting Dashboard**: Analytics and insights
9. **Mobile App**: React Native mobile application
10. **Multi-language Support**: i18n implementation

### Technical Improvements
- Implement caching (Redis)
- Add real-time updates (SignalR)
- Implement file upload for design references
- Add image storage for order photos
- Implement audit logging
- Add backup and restore functionality

---

## Conclusion

This tailoring management system implements modern web development practices with:
- Clean Architecture for maintainability
- JWT authentication for security
- Role-based access control
- Responsive design with React Bootstrap
- Type-safe development with TypeScript
- RESTful API design
- Comprehensive error handling
- Professional UI/UX with gradients and animations

The system is production-ready and scalable for small to medium-sized tailoring businesses.

---

**Document Version**: 1.0  
**Last Updated**: November 29, 2025  
**Maintained By**: Development Team
