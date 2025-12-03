# Tailoring Management System - Project Demo Documentation

## 📋 Table of Contents

1. [Project Overview](#project-overview)
2. [Architecture & Tech Stack](#architecture--tech-stack)
3. [Backend Explanation](#backend-explanation)
4. [Frontend Explanation](#frontend-explanation)
5. [Key Features & Code Walkthrough](#key-features--code-walkthrough)
6. [Database Design](#database-design)
7. [API Endpoints](#api-endpoints)
8. [Security Implementation](#security-implementation)
9. [Demo Flow](#demo-flow)

---

## 🎯 Project Overview

**Project Name:** Tailoring Management System  
**Type:** Full-Stack Web Application  
**Purpose:** Complete ERP system for tailoring shops to manage customers, orders, measurements, and payments

**Key Capabilities:**

- User authentication (Admin & Customer roles)
- Customer management with detailed measurements
- Order creation and tracking
- Payment management (advance payment & balance)
- Order progress tracking
- Professional ERP-style UI

---

## 🏗️ Architecture & Tech Stack

### Backend Architecture

```
Clean Architecture Pattern (Layered Architecture)

┌─────────────────────────────────────┐
│     Tailoring.API (Layer 1)        │  ← Presentation Layer
│  - Controllers                      │
│  - JWT Authentication               │
│  - Dependency Injection             │
└─────────────────────────────────────┘
              ↓
┌─────────────────────────────────────┐
│     Tailoring.Core (Layer 2)       │  ← Business Logic Layer
│  - Entities (Domain Models)        │
│  - DTOs (Data Transfer Objects)    │
│  - Interfaces (Contracts)          │
│  - Services (Business Logic)       │
└─────────────────────────────────────┘
              ↓
┌─────────────────────────────────────┐
│  Tailoring.Infrastructure (Layer 3)│  ← Data Access Layer
│  - DbContext (Entity Framework)    │
│  - Repositories (Data Operations)  │
│  - Migrations (Database Schema)    │
└─────────────────────────────────────┘
```

### Technology Stack

**Backend:**

- **.NET Core 9.0** - Modern, cross-platform framework
- **Entity Framework Core 9.0** - ORM for database operations
- **SQL Server** - Relational database
- **JWT Authentication** - Secure token-based auth
- **BCrypt.Net** - Password hashing

**Frontend:**

- **React 19.2.0** - Modern UI library with hooks
- **TypeScript** - Type-safe JavaScript
- **Redux Toolkit** - State management
- **React Router DOM** - Client-side routing
- **Formik + Yup** - Form validation
- **React Bootstrap** - UI components
- **Axios** - HTTP client

---

## 🔧 Backend Explanation

### 1. Program.cs - Application Entry Point

```csharp
var builder = WebApplication.CreateBuilder(args);

// Service Registration
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
```

**Explanation:**

- `WebApplication.CreateBuilder` - Creates the application host
- `AddDbContext` - Registers database context with dependency injection
- `UseSqlServer` - Configures Entity Framework to use SQL Server
- Connection string comes from `appsettings.json`

```csharp
builder.Services.AddScoped<IRepository<Customer>, Repository<Customer>>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
```

**Explanation:**

- `AddScoped` - Creates one instance per HTTP request
- `IRepository<T>` - Generic repository pattern for data access
- `ICustomerService` - Business logic interface
- This follows **Dependency Inversion Principle** (SOLID)

```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });
```

**Explanation:**

- **JWT Bearer Authentication** - Token-based security
- `ValidateIssuer` - Ensures token comes from our server
- `ValidateAudience` - Ensures token is for our application
- `ValidateLifetime` - Checks token expiration
- `IssuerSigningKey` - Secret key to verify token signature
- Prevents token tampering and unauthorized access

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp",
        policy =>
        {
            policy.WithOrigins("http://localhost:3001")
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
});
```

**Explanation:**

- **CORS** - Cross-Origin Resource Sharing
- Allows React app (port 3001) to call backend API (port 5167)
- `AllowAnyHeader` - Accepts all HTTP headers
- `AllowAnyMethod` - Allows GET, POST, PUT, DELETE
- `AllowCredentials` - Allows cookies/auth tokens

---

### 2. Entities - Domain Models

#### BaseEntity.cs

```csharp
public abstract class BaseEntity
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
```

**Explanation:**

- `abstract` - Cannot be instantiated directly, only inherited
- All entities inherit common properties: Id, timestamps
- `DateTime.UtcNow` - Uses UTC to avoid timezone issues
- `DateTime?` - Nullable, UpdatedAt is null until first update

#### Customer.cs

```csharp
public class Customer : BaseEntity
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;

    // Navigation properties
    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public Measurement? Measurements { get; set; }
}
```

**Explanation:**

- Inherits from `BaseEntity` (gets Id, CreatedAt, UpdatedAt)
- `string.Empty` - Default initialization prevents null reference errors
- `ICollection<Order>` - One customer can have many orders (1-to-many relationship)
- `Measurement?` - Nullable, customer may not have measurements yet
- Navigation properties enable **Entity Framework relationships**

#### Order.cs

```csharp
public class Order : BaseEntity
{
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public string GarmentType { get; set; } = string.Empty;
    public string FabricType { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public string Priority { get; set; } = "Medium";

    // Payment properties
    public decimal TotalAmount { get; set; }
    public decimal AdvancePaid { get; set; }

    // Navigation
    public ICollection<OrderProgress> ProgressUpdates { get; set; } = new List<OrderProgress>();
}
```

**Explanation:**

- **Foreign Key Pattern**: `CustomerId` (FK) + `Customer` (navigation)
- `null!` - Tells compiler "trust me, EF will populate this"
- `OrderStatus` - Enum for type safety (Pending, InProgress, Completed, etc.)
- `decimal` - Precise type for money calculations (no floating-point errors)
- `ProgressUpdates` - Tracks order progress history

---

### 3. Services - Business Logic

#### AuthService.cs

```csharp
public class AuthService : IAuthService
{
    private readonly IRepository<User> _userRepository;
    private readonly IConfiguration _configuration;

    public AuthService(IRepository<User> userRepository, IConfiguration configuration)
    {
        _userRepository = userRepository;
        _configuration = configuration;
    }
```

**Explanation:**

- **Constructor Injection** - Dependencies injected automatically
- `IRepository<User>` - Generic repository for user data access
- `IConfiguration` - Access to appsettings.json for JWT settings

```csharp
public async Task<User?> RegisterAsync(string username, string email, string password, string role)
{
    // Check if user exists
    var existingUser = (await _userRepository.GetAllAsync())
        .FirstOrDefault(u => u.Email == email || u.Username == username);

    if (existingUser != null)
        return null;

    // Hash password
    var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

    var user = new User
    {
        Username = username,
        Email = email,
        PasswordHash = passwordHash,
        Role = role
    };

    return await _userRepository.AddAsync(user);
}
```

**Explanation:**

- **async/await** - Non-blocking operations, improves scalability
- **Duplicate Check** - Prevents multiple users with same email/username
- **BCrypt Hashing** - One-way encryption, even admins can't see passwords
- BCrypt automatically generates salt and handles iterations
- Returns `null` on failure (duplicate), `User` on success

```csharp
public async Task<string?> LoginAsync(string username, string password)
{
    var users = await _userRepository.GetAllAsync();
    var user = users.FirstOrDefault(u => u.Username == username);

    if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        return null;

    return GenerateJwtToken(user);
}
```

**Explanation:**

- Finds user by username
- `BCrypt.Verify` - Compares plain password with hashed password
- Returns `null` if credentials invalid, JWT token if valid
- **Never stores plain passwords** - security best practice

```csharp
private string GenerateJwtToken(User user)
{
    var securityKey = new SymmetricSecurityKey(
        Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
    var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

    var claims = new[]
    {
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(ClaimTypes.Name, user.Username),
        new Claim(ClaimTypes.Email, user.Email),
        new Claim(ClaimTypes.Role, user.Role)
    };

    var token = new JwtSecurityToken(
        issuer: _configuration["Jwt:Issuer"],
        audience: _configuration["Jwt:Audience"],
        claims: claims,
        expires: DateTime.Now.AddHours(24),
        signingCredentials: credentials
    );

    return new JwtSecurityTokenHandler().WriteToken(token);
}
```

**Explanation:**

- **JWT Structure**: Header + Payload + Signature
- `Claims` - User information embedded in token (id, name, email, role)
- `expires: AddHours(24)` - Token valid for 24 hours
- `HmacSha256` - Signing algorithm prevents tampering
- Token is **stateless** - no server-side session storage needed
- Frontend sends this token in `Authorization: Bearer <token>` header

---

### 4. Controllers - API Endpoints

#### AuthController.cs

```csharp
[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }
```

**Explanation:**

- `[Route("api/[controller]")]` - Creates route: `/api/Auth`
- `[ApiController]` - Enables automatic model validation, binding
- Constructor injection of `IAuthService`

```csharp
[HttpPost("register")]
public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
{
    var user = await _authService.RegisterAsync(
        registerDto.Username,
        registerDto.Email,
        registerDto.Password,
        registerDto.Role ?? "Customer"
    );

    if (user == null)
        return BadRequest(new { message = "User already exists" });

    return Ok(new
    {
        message = "User registered successfully",
        userId = user.Id,
        username = user.Username
    });
}
```

**Explanation:**

- `[HttpPost("register")]` - POST request to `/api/Auth/register`
- `[FromBody]` - Deserializes JSON request body to `RegisterDto`
- `??` - Null coalescing, defaults role to "Customer"
- `BadRequest(400)` - Returns 400 status code
- `Ok(200)` - Returns 200 status code with JSON response
- **DTO Pattern** - Never expose entities directly to API

```csharp
[HttpPost("login")]
public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
{
    var token = await _authService.LoginAsync(loginDto.Username, loginDto.Password);

    if (token == null)
        return Unauthorized(new { message = "Invalid credentials" });

    return Ok(new { token });
}
```

**Explanation:**

- Returns JWT token on successful login
- `Unauthorized(401)` - Standard HTTP status for failed auth
- Frontend stores token in Redux store and localStorage
- All subsequent requests include this token

#### OrdersController.cs

```csharp
[Authorize]
[Route("api/[controller]")]
[ApiController]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }
```

**Explanation:**

- `[Authorize]` - All endpoints require valid JWT token
- Automatically validates token before executing any action
- If token invalid/expired, returns 401 Unauthorized

```csharp
[HttpGet]
public async Task<ActionResult<IEnumerable<OrderDto>>> GetAllOrders()
{
    var orders = await _orderService.GetAllOrdersAsync();
    return Ok(orders);
}
```

**Explanation:**

- GET `/api/Orders` - Returns all orders
- `ActionResult<IEnumerable<OrderDto>>` - Strongly typed return
- Returns list of `OrderDto` (not entities) - **DTO Pattern**

```csharp
[HttpGet("{id}")]
public async Task<ActionResult<OrderDto>> GetOrder(int id)
{
    var order = await _orderService.GetOrderByIdAsync(id);

    if (order == null)
        return NotFound(new { message = "Order not found" });

    return Ok(order);
}
```

**Explanation:**

- GET `/api/Orders/5` - Gets order with ID 5
- `{id}` - Route parameter automatically bound
- `NotFound(404)` - Proper HTTP status for missing resource

```csharp
[HttpPost]
public async Task<ActionResult<OrderDto>> CreateOrder([FromBody] CreateOrderDto createOrderDto)
{
    try
    {
        var order = await _orderService.CreateOrderAsync(createOrderDto);
        return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, order);
    }
    catch (Exception ex)
    {
        return BadRequest(new { message = ex.Message });
    }
}
```

**Explanation:**

- POST `/api/Orders` - Creates new order
- `CreatedAtAction` - Returns 201 status with location header
- Location header: `/api/Orders/{newOrderId}`
- `try-catch` - Handles business logic exceptions gracefully

```csharp
[HttpPut("{id}")]
public async Task<IActionResult> UpdateOrder(int id, [FromBody] UpdateOrderDto updateOrderDto)
{
    try
    {
        await _orderService.UpdateOrderAsync(id, updateOrderDto);
        return NoContent();
    }
    catch (KeyNotFoundException)
    {
        return NotFound(new { message = "Order not found" });
    }
    catch (Exception ex)
    {
        return BadRequest(new { message = ex.Message });
    }
}
```

**Explanation:**

- PUT `/api/Orders/5` - Updates order 5
- `NoContent(204)` - Standard for successful update with no return data
- Different exceptions mapped to different HTTP status codes

---

### 5. Database Context & Migrations

#### AppDbContext.cs

```csharp
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Customer> Customers { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<Measurement> Measurements { get; set; }
    public DbSet<OrderProgress> OrderProgress { get; set; }
```

**Explanation:**

- `DbContext` - Entity Framework's database session
- `DbSet<T>` - Represents a table in the database
- Each DbSet becomes a table: Users, Customers, Orders, etc.

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);

    // Configure relationships
    modelBuilder.Entity<Order>()
        .HasOne(o => o.Customer)
        .WithMany(c => c.Orders)
        .HasForeignKey(o => o.CustomerId)
        .OnDelete(DeleteBehavior.Cascade);
```

**Explanation:**

- **Fluent API** - Configures entity relationships and constraints
- `HasOne...WithMany` - 1-to-many relationship
- `HasForeignKey` - Specifies the foreign key column
- `OnDelete(Cascade)` - Deleting customer deletes all their orders
- Alternative: `Restrict`, `SetNull`

```csharp
    modelBuilder.Entity<Measurement>()
        .HasOne(m => m.Customer)
        .WithOne(c => c.Measurements)
        .HasForeignKey<Measurement>(m => m.CustomerId);
```

**Explanation:**

- `HasOne...WithOne` - 1-to-1 relationship
- Customer can have only one Measurement record
- `HasForeignKey<Measurement>` - FK is in Measurement table

```csharp
    // Configure decimal precision for money fields
    modelBuilder.Entity<Order>()
        .Property(o => o.TotalAmount)
        .HasColumnType("decimal(18,2)");

    modelBuilder.Entity<Order>()
        .Property(o => o.AdvancePaid)
        .HasColumnType("decimal(18,2)");
}
```

**Explanation:**

- `decimal(18,2)` - 18 total digits, 2 after decimal point
- Essential for money - prevents rounding errors
- Example: Can store up to 9,999,999,999,999.99

#### Migrations

```bash
Add-Migration InitialCreate
Update-Database
```

**Explanation:**

- `Add-Migration` - Creates migration file with schema changes
- `Update-Database` - Applies migration to database
- Migrations track all schema changes over time
- Can rollback to previous versions

---

## 🎨 Frontend Explanation

### 1. Application Entry Point

#### index.tsx

```typescript
import React from "react";
import ReactDOM from "react-dom/client";
import { Provider } from "react-redux";
import { BrowserRouter } from "react-router-dom";
import { store } from "./store/store";
import App from "./App";
import "bootstrap/dist/css/bootstrap.min.css";
import "./index.css";

const root = ReactDOM.createRoot(
  document.getElementById("root") as HTMLElement
);

root.render(
  <React.StrictMode>
    <Provider store={store}>
      <BrowserRouter>
        <App />
      </BrowserRouter>
    </Provider>
  </React.StrictMode>
);
```

**Explanation:**

- `ReactDOM.createRoot` - React 18+ rendering method
- `<Provider store={store}>` - Makes Redux store available to all components
- `<BrowserRouter>` - Enables client-side routing
- `<React.StrictMode>` - Development mode, highlights potential problems
- Bootstrap CSS imported globally

---

### 2. Redux State Management

#### store/authSlice.ts

```typescript
import { createSlice, PayloadAction } from "@reduxjs/toolkit";

interface AuthState {
  token: string | null;
  isAuthenticated: boolean;
}

const initialState: AuthState = {
  token: localStorage.getItem("token"),
  isAuthenticated: !!localStorage.getItem("token"),
};
```

**Explanation:**

- **Redux Slice** - Combines actions and reducer in one file
- `AuthState` - TypeScript interface for type safety
- `localStorage.getItem('token')` - Persists auth across page refreshes
- `!!` - Double negation converts string to boolean

```typescript
const authSlice = createSlice({
  name: "auth",
  initialState,
  reducers: {
    setCredentials: (state, action: PayloadAction<{ token: string }>) => {
      state.token = action.payload.token;
      state.isAuthenticated = true;
      localStorage.setItem("token", action.payload.token);
    },
    logout: (state) => {
      state.token = null;
      state.isAuthenticated = false;
      localStorage.removeItem("token");
    },
  },
});
```

**Explanation:**

- `createSlice` - Automatically generates action creators
- `PayloadAction<T>` - Types the action payload
- **Immer.js** built-in - Can "mutate" state safely (actually creates new state)
- `localStorage.setItem/removeItem` - Persists across browser sessions
- Actions: `setCredentials(token)` and `logout()`

```typescript
export const { setCredentials, logout } = authSlice.actions;
export default authSlice.reducer;

// Selectors
export const selectAuth = (state: RootState) => state.auth;
export const selectToken = (state: RootState) => state.auth.token;
```

**Explanation:**

- Export actions for components to dispatch
- Export reducer for store configuration
- **Selectors** - Functions to access state slices
- TypeScript ensures type safety throughout

---

### 3. API Service Configuration

#### services/api.ts

```typescript
import axios from "axios";

const API_BASE_URL = "http://localhost:5167/api";

const api = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    "Content-Type": "application/json",
  },
});
```

**Explanation:**

- `axios.create` - Creates configured Axios instance
- All requests automatically use base URL
- Default content type for JSON

```typescript
// Request interceptor - adds auth token
api.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem("token");
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => {
    return Promise.reject(error);
  }
);
```

**Explanation:**

- **Interceptor** - Runs before every request
- Automatically adds `Authorization: Bearer <token>` header
- No need to manually add token to each request
- If token exists, all API calls are authenticated

```typescript
// Response interceptor - handles errors
api.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      // Unauthorized - clear auth and redirect
      localStorage.removeItem("token");
      window.location.href = "/login";
    }
    return Promise.reject(error);
  }
);
```

**Explanation:**

- Runs after every response
- Status 401 - Token expired/invalid
- Automatically logs out user and redirects to login
- **Centralized error handling** - DRY principle

---

### 4. Form Validation with Formik

#### OrderForm.tsx - Validation Schema

```typescript
const orderValidationSchema = Yup.object().shape({
  // Customer Information
  customerFirstName: Yup.string()
    .min(2, 'First name must be at least 2 characters')
    .max(50, 'First name must not exceed 50 characters')
    .required('First name is required'),

  customerLastName: Yup.string()
    .min(2, 'Last name must be at least 2 characters')
    .max(50, 'Last name must not exceed 50 characters')
    .required('Last name is required'),

  customerEmail: Yup.string()
    .email('Invalid email format')
    .required('Email is required'),

  customerPhone: Yup.string()
    .matches(/^[\d\s\-\+\(\)]+$/, 'Invalid phone number format')
    .min(10, 'Phone number must be at least 10 digits')
    .required('Phone number is required'),
```

**Explanation:**

- **Yup** - Schema validation library
- `.string()` - Data type validation
- `.min(2)` - Minimum length with custom error message
- `.max(50)` - Maximum length constraint
- `.email()` - Built-in email format validation
- `.matches(regex)` - Phone number pattern matching
- `.required()` - Field cannot be empty
- Runs validation on blur and submit

```typescript
  // Order Details
  garmentType: Yup.string()
    .min(2, 'Garment type must be at least 2 characters')
    .required('Garment type is required'),

  fabricType: Yup.string()
    .min(2, 'Fabric type must be at least 2 characters')
    .required('Fabric type is required'),

  dueDate: Yup.date()
    .min(new Date(), 'Due date cannot be in the past')
    .required('Due date is required'),

  priority: Yup.string()
    .oneOf(['Low', 'Medium', 'High', 'Urgent'], 'Invalid priority')
    .required('Priority is required'),
```

**Explanation:**

- `.date()` - Date validation
- `.min(new Date())` - Must be today or future
- `.oneOf([...])` - Value must be in enum list
- Prevents invalid data entry before API call

```typescript
  // Payment Information
  totalAmount: Yup.number()
    .min(0, 'Total amount cannot be negative')
    .required('Total amount is required'),

  advancePaid: Yup.number()
    .min(0, 'Advance payment cannot be negative')
    .test('max-advance', 'Advance cannot exceed total amount', function(value) {
      const { totalAmount } = this.parent;
      return value <= totalAmount;
    })
    .required('Advance payment is required'),
```

**Explanation:**

- `.number()` - Numeric validation
- `.min(0)` - Cannot be negative
- `.test()` - Custom validation logic
- `this.parent` - Access other field values
- Ensures advance ≤ total amount - **Business rule validation**

#### OrderForm.tsx - Formik Implementation

```typescript
<Formik
  initialValues={initialValues}
  validationSchema={orderValidationSchema}
  onSubmit={handleSubmit}
  validateOnChange={true}
  validateOnBlur={true}
>
  {({ values, errors, touched, isSubmitting, setFieldValue, handleSubmit, handleBlur }) => (
    <Form noValidate onSubmit={handleSubmit}>
```

**Explanation:**

- `initialValues` - Default form state
- `validationSchema` - Yup schema for validation
- `onSubmit` - Function called when form is valid
- `validateOnChange` - Validates while typing
- `validateOnBlur` - Validates when leaving field
- `noValidate` - Disables browser validation, uses Formik
- **Render props pattern** - Function receives form state

```typescript
<Form.Group className="mb-3">
  <Form.Label>
    First Name <span className="text-danger">*</span>
  </Form.Label>
  <Form.Control
    type="text"
    name="customerFirstName"
    placeholder="Enter first name"
    value={values.customerFirstName}
    onChange={(e) => setFieldValue("customerFirstName", e.target.value)}
    onBlur={handleBlur}
    isInvalid={touched.customerFirstName && !!errors.customerFirstName}
    disabled={isSubmitting}
  />
  <Form.Control.Feedback type="invalid">
    {errors.customerFirstName}
  </Form.Control.Feedback>
</Form.Group>
```

**Explanation:**

- `values.customerFirstName` - Current field value from Formik
- `setFieldValue` - Updates Formik state
- `handleBlur` - Marks field as touched, triggers validation
- `isInvalid` - Shows red border if touched AND has error
- `!!errors.customerFirstName` - Converts to boolean
- `disabled={isSubmitting}` - Prevents changes during submit
- `Form.Control.Feedback` - Shows error message below field

---

### 5. Component Structure

#### Dashboard.tsx

```typescript
interface Order {
  id: number;
  customerName: string;
  garmentType: string;
  fabricType: string;
  status: string;
  priority: string;
  dueDate: string;
  totalAmount: number;
  advancePaid: number;
  createdAt: string;
}

const Dashboard: React.FC = () => {
  const [orders, setOrders] = useState<Order[]>([]);
  const [loading, setLoading] = useState(true);
  const [currentPage, setCurrentPage] = useState(1);
  const ordersPerPage = 9;
```

**Explanation:**

- **TypeScript interface** - Defines Order shape for type safety
- `React.FC` - Functional Component type
- `useState<Order[]>` - Typed state for orders array
- `loading` - Shows spinner while fetching data
- Pagination: 9 orders per page

```typescript
useEffect(() => {
  fetchOrders();
}, []);

const fetchOrders = async () => {
  try {
    setLoading(true);
    const response = await api.get<Order[]>("/orders");
    setOrders(response.data);
  } catch (error) {
    console.error("Error fetching orders:", error);
    alert("Failed to load orders");
  } finally {
    setLoading(false);
  }
};
```

**Explanation:**

- `useEffect` - Runs after component mounts
- `[]` dependency - Runs only once
- `async/await` - Asynchronous API call
- `api.get<Order[]>` - TypeScript generic for response type
- `try-catch-finally` - Error handling
- `finally` - Runs regardless of success/failure

```typescript
// Pagination logic
const indexOfLastOrder = currentPage * ordersPerPage;
const indexOfFirstOrder = indexOfLastOrder - ordersPerPage;
const currentOrders = orders.slice(indexOfFirstOrder, indexOfLastOrder);
const totalPages = Math.ceil(orders.length / ordersPerPage);
```

**Explanation:**

- **Pagination math**:
  - Page 1: orders 0-8 (9 items)
  - Page 2: orders 9-17 (9 items)
- `slice(start, end)` - Extracts subset of array
- `Math.ceil` - Rounds up for last partial page

```typescript
const paginate = (pageNumber: number) => {
  setCurrentPage(pageNumber);
  window.scrollTo({ top: 0, behavior: "smooth" });
};
```

**Explanation:**

- Changes page number
- `window.scrollTo` - Scrolls to top smoothly
- Better UX when navigating pages

```typescript
const getStatusBadge = (status: string) => {
  const statusColors: { [key: string]: string } = {
    Pending: "warning",
    InProgress: "info",
    Completed: "success",
    Delivered: "primary",
    Cancelled: "danger",
  };
  return statusColors[status] || "secondary";
};
```

**Explanation:**

- **Object lookup** - Maps status to Bootstrap color
- `statusColors[status]` - Gets color by key
- `|| 'secondary'` - Fallback for unknown status
- DRY - Avoids repetitive if-else statements

#### Pagination Component

```typescript
<div className="pagination-container">
  <button
    className="pagination-btn"
    onClick={() => paginate(currentPage - 1)}
    disabled={currentPage === 1}
  >
    <i className="bi bi-chevron-left"></i> Previous
  </button>

  <div className="pagination-numbers">
    {currentPage > 2 && (
      <>
        <button onClick={() => paginate(1)}>1</button>
        {currentPage > 3 && <span className="pagination-ellipsis">...</span>}
      </>
    )}

    {[currentPage - 1, currentPage, currentPage + 1]
      .filter((page) => page > 0 && page <= totalPages)
      .map((page) => (
        <button
          key={page}
          onClick={() => paginate(page)}
          className={page === currentPage ? "active" : ""}
        >
          {page}
        </button>
      ))}

    {currentPage < totalPages - 1 && (
      <>
        {currentPage < totalPages - 2 && (
          <span className="pagination-ellipsis">...</span>
        )}
        <button onClick={() => paginate(totalPages)}>{totalPages}</button>
      </>
    )}
  </div>

  <button
    className="pagination-btn"
    onClick={() => paginate(currentPage + 1)}
    disabled={currentPage === totalPages}
  >
    Next <i className="bi bi-chevron-right"></i>
  </button>
</div>
```

**Explanation:**

- **Smart pagination** - Shows current, previous, next pages
- `currentPage > 2` - Shows page 1 if far from start
- Ellipsis (`...`) for large gaps
- `filter` - Only shows valid page numbers
- `.active` class highlights current page
- Previous/Next buttons disabled at boundaries
- Example: Page 5 of 10 shows: 1 ... 4 5 6 ... 10

---

### 6. Routing & Protected Routes

#### App.tsx

```typescript
import { Routes, Route, Navigate } from "react-router-dom";
import { useSelector } from "react-redux";
import { selectAuth } from "./store/authSlice";

function App() {
  const { isAuthenticated } = useSelector(selectAuth);

  return (
    <Routes>
      <Route path="/login" element={<Login />} />
      <Route path="/register" element={<Register />} />

      <Route
        path="/dashboard"
        element={isAuthenticated ? <Dashboard /> : <Navigate to="/login" />}
      />

      <Route
        path="/orders/new"
        element={isAuthenticated ? <OrderForm /> : <Navigate to="/login" />}
      />

      <Route path="/" element={<Navigate to="/dashboard" />} />
    </Routes>
  );
}
```

**Explanation:**

- `useSelector(selectAuth)` - Gets auth state from Redux
- **Protected Routes** - Checks `isAuthenticated` before rendering
- `<Navigate to="/login" />` - Redirects if not authenticated
- Ternary operator: `condition ? true : false`
- Default route `/` redirects to dashboard
- **Client-side routing** - No page refresh, instant navigation

---

### 7. Styling & Design System

#### Global Styles (`index.css` + `App.css`)

```
Theme: Navy blue professional ERP style
Primary: #1a365d → #2c5282 → #4a90e2
Font: Inter, system-ui
Effects: Glassmorphism cards, soft shadows, subtle animations
```

Key choices:

- Global variables for colors and spacing ensure consistency across components.
- Button gradients and hover transitions provide modern, responsive feedback.
- Form controls styled for clear validation states (red borders + messages).
- Custom scrollbar and subtle page transitions improve perceived performance.

Example highlights:

```css
:root {
  --primary-900: #1a365d;
  --primary-700: #2c5282;
  --primary-500: #4a90e2;
  --bg-soft: rgba(255, 255, 255, 0.6);
}

.card-glass {
  background: var(--bg-soft);
  backdrop-filter: blur(12px);
  box-shadow: 0 8px 24px rgba(0, 0, 0, 0.12);
  transition: transform 0.2s ease, box-shadow 0.2s ease;
}
.card-glass:hover {
  transform: translateY(-2px);
}
```

#### Page Styles (Login, Register, Dashboard, Customer Portal)

- Consistent header sections with gradients, icons, and action buttons.
- Dashboard stat cards use floating animations and color-coded badges.
- Pagination styled for clarity with ellipsis for large datasets.

Why it matters for the demo:

- Shows attention to UX while keeping performance and accessibility.
- Professional look aligns with ERP expectations and clarity for business users.

---

### 8. Frontend Code Reference (File-by-File)

This section gives concise, code-level explanations you can narrate during the demo. Paths refer to `Frontend/src/...` unless noted.

- `index.tsx`

  - Bootstraps React with `<Provider>` and `<BrowserRouter>`.
  - Imports global CSS and Bootstrap; renders `<App />`.
  - Key point: App-wide access to Redux store and routes.

- `App.tsx`

  - Declares routes: `/login`, `/register`, `/dashboard`, `/orders/new`.
  - Protected routes using `isAuthenticated` from Redux.
  - Key point: Frontend authorization gate via client-side routing.

- `store/authSlice.ts`

  - Holds `token` and `isAuthenticated` state; persists token in `localStorage`.
  - Actions: `setCredentials`, `logout`; Selectors: `selectAuth`, `selectToken`.
  - Key point: Predictable auth state with Redux Toolkit.

- `services/api.ts`

  - Axios instance with `baseURL` pointing to API (`http://localhost:5167/api`).
  - Request interceptor adds `Authorization: Bearer <token>` if present.
  - Response interceptor handles `401` by clearing token and redirecting to `/login`.
  - Key point: Centralized, DRY API configuration and error handling.

- `components/orders/OrderForm.tsx`

  - Uses Formik with Yup `orderValidationSchema` for robust validation.
  - Field binding pattern:
    - Value: `values.fieldName`
    - Change: `onChange={(e) => setFieldValue('fieldName', e.target.value)}`
    - Blur: `onBlur={handleBlur}` to mark touched and trigger validation
    - Invalid state: `isInvalid={touched.fieldName && !!errors.fieldName}`
    - Error text: `<Form.Control.Feedback type="invalid">{errors.fieldName}</Form.Control.Feedback>`
  - Submit flow:
    - `<Form noValidate onSubmit={handleSubmit}>` connects Bootstrap Form to Formik.
    - `handleSubmit` validates; if valid, creates Customer then Order via API.
  - Business rules:
    - `advancePaid` must be `<= totalAmount` (Yup `.test(...)`).
    - `dueDate` cannot be in the past.
  - Key point: Validation prevents empty/invalid submissions; clear user feedback.

- `pages/Dashboard.tsx` (or `components/...` depending on structure)

  - Fetches orders via `api.get('/orders')`; stores in local state.
  - Pagination math and UX: slices orders; smooth scroll on page change.
  - Status badge mapping via lookup for consistent visuals.
  - Key point: TypeScript interfaces document data shape (`Order`), reducing runtime errors.

- `styles/App.css`, `index.css`, and page-specific CSS (`Login.css`, `Register.css`, `Dashboard.css`, `CustomerPortal.css`)
  - Theme variables for primary colors; glassmorphism card style; responsive layout.
  - Animations: subtle hover/float; clear invalid states on inputs.
  - Key point: Professional, consistent design system enhances demo impact.

Optional items to highlight live:

- Show token in DevTools → Application → Local Storage (`token`).
- Trigger a `401` by removing token and attempting an API call → auto-redirect.
- Demonstrate validation by submitting empty order form → errors appear; fix fields → errors clear; submit succeeds.

---

### 9. Frontend Live Demo Lines & Code Anchors

Use these short lines during the demo, with quick file jumps.

- `src/index.tsx`

  - Line: render root → “Here we bootstrap React, attach Redux and Router so the whole app has state and navigation.”
  - Anchor: `ReactDOM.createRoot(...); <Provider store={store}> <BrowserRouter> <App />`

- `src/App.tsx`

  - Line: protected routes → “Routes are guarded by Redux auth; unauthenticated users are redirected to login.”
  - Anchor: `<Route path="/dashboard" element={isAuthenticated ? <Dashboard /> : <Navigate to="/login" />}/>`

- `src/store/authSlice.ts`

  - Line: token persistence → “We persist the JWT in localStorage and reflect it in isAuthenticated.”
  - Anchor: `initialState` and `setCredentials` writing `localStorage.setItem('token', ...)`

- `src/services/api.ts`

  - Line: interceptor → “Every request gets the Bearer token; 401s auto-logout and redirect.”
  - Anchor: `api.interceptors.request.use(...)` and `api.interceptors.response.use(...)`

- `src/components/orders/OrderForm.tsx`

  - Line: Formik submit wiring → “Bootstrap Form uses Formik’s handleSubmit; Yup blocks invalid submissions.”
  - Anchor: `<Form noValidate onSubmit={handleSubmit}>` and `validationSchema={orderValidationSchema}`
  - Line: field binding → “Each field binds value, change, blur, invalid state, and error text for clear feedback.”
  - Anchor: `value={values.customerFirstName}`, `onChange={...setFieldValue(...)}`, `onBlur={handleBlur}`, `isInvalid={touched... && !!errors...}`
  - Line: business rule → “Advance cannot exceed total; due date can’t be past.”
  - Anchor: `advancePaid: Yup.number().test('max-advance', ...)`, `dueDate: Yup.date().min(new Date(), ...)`

- `src/pages/Dashboard.tsx` (or component file)

  - Line: fetching + typing → “TypeScript models the order shape; data loads via Axios with proper error handling.”
  - Anchor: `interface Order { ... }`, `const response = await api.get<Order[]>('/orders')`
  - Line: pagination → “Index math slices the array; we smooth-scroll on page change for UX.”
  - Anchor: `indexOfFirstOrder/indexOfLastOrder`, `orders.slice(...)`, `window.scrollTo({ behavior: 'smooth' })`

- Styling (`src/App.css`, `src/index.css`, page CSS)
  - Line: theme → “CSS variables unify the ERP blue palette; glass cards, subtle animations improve clarity.”
  - Anchor: `:root { --primary-900/... }`, `.card-glass { backdrop-filter: blur(12px); }`

Quick checks during demo:

- Show token: DevTools → Application → Local Storage → `token`.
- Force 401: remove token → navigate to dashboard → auto-redirect to `/login`.
- Validation: submit empty order → errors; fix inputs → submit succeeds → see new order on dashboard.

---

## 🔐 Security Implementation

### 1. Password Security

```csharp
// Backend - AuthService.cs
var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);
```

- **BCrypt hashing** - Industry standard
- Automatically handles salt generation
- Computationally expensive (prevents brute force)
- One-way encryption (irreversible)

### 2. JWT Authentication

```csharp
// Token generation with claims
var claims = new[]
{
    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
    new Claim(ClaimTypes.Role, user.Role)
};
```

- **Stateless authentication** - No server session
- Claims embedded in token
- Signed with secret key - prevents tampering
- 24-hour expiration

### 3. Authorization

```csharp
[Authorize]
public class OrdersController : ControllerBase
```

- Validates JWT on every request
- Returns 401 if invalid/expired
- Role-based access can be added: `[Authorize(Roles = "Admin")]`

### 4. CORS Protection

```csharp
policy.WithOrigins("http://localhost:3001")
      .AllowCredentials();
```

- Only allows requests from React app
- Prevents unauthorized cross-origin requests

### 5. Input Validation

```typescript
// Frontend - Yup validation
customerEmail: Yup.string().email().required();
```

```csharp
// Backend - Model validation
[Required]
[EmailAddress]
public string Email { get; set; }
```

- **Defense in depth** - Validated on both sides
- Prevents SQL injection, XSS attacks

---

## 💾 Database Design

### Entity Relationship Diagram

```
┌─────────────┐         ┌─────────────┐
│    User     │         │  Customer   │
├─────────────┤         ├─────────────┤
│ Id (PK)     │         │ Id (PK)     │
│ Username    │         │ FirstName   │
│ Email       │         │ LastName    │
│ PasswordHash│         │ Email       │
│ Role        │         │ Phone       │
└─────────────┘         └─────────────┘
                              │ 1
                              │
                              │ 1:M
                              ▼ M
                        ┌─────────────┐
                        │    Order    │
                        ├─────────────┤
                        │ Id (PK)     │
                        │ CustomerId  │◄─── Foreign Key
                        │ GarmentType │
                        │ FabricType  │
                        │ DueDate     │
                        │ Status      │
                        │ TotalAmount │
                        │ AdvancePaid │
                        └─────────────┘
                              │ 1
                              │
                              │ 1:M
                              ▼ M
                        ┌──────────────┐
                        │OrderProgress │
                        ├──────────────┤
                        │ Id (PK)      │
                        │ OrderId      │◄─── Foreign Key
                        │ Status       │
                        │ Description  │
                        │ UpdatedBy    │
                        └──────────────┘

┌─────────────┐
│ Measurement │
├─────────────┤
│ Id (PK)     │
│ CustomerId  │◄─── Foreign Key (1:1)
│ Chest       │
│ Waist       │
│ Height      │
└─────────────┘
```

### Relationships

1. **User** - Independent, manages authentication
2. **Customer → Orders** - One customer, many orders (1:M)
3. **Order → OrderProgress** - One order, many progress updates (1:M)
4. **Customer → Measurement** - One customer, one measurement (1:1)

### Cascade Deletes

- Deleting customer → Deletes all their orders
- Deleting order → Deletes all progress updates

---

## 🌐 API Endpoints

### Authentication Endpoints

```
POST /api/Auth/register
Body: { username, email, password, role? }
Response: { message, userId, username }

POST /api/Auth/login
Body: { username, password }
Response: { token }
```

### Customer Endpoints

```
GET /api/Customers
Headers: Authorization: Bearer <token>
Response: Customer[]

GET /api/Customers/{id}
Response: Customer

POST /api/Customers
Body: { firstName, lastName, email, phone }
Response: Customer

PUT /api/Customers/{id}
Body: { firstName, lastName, email, phone }
Response: 204 No Content

DELETE /api/Customers/{id}
Response: 204 No Content
```

### Order Endpoints

```
GET /api/Orders
Response: OrderDto[]

GET /api/Orders/{id}
Response: OrderDto

POST /api/Orders
Body: {
  customerId,
  garmentType,
  fabricType,
  dueDate,
  priority,
  totalAmount,
  advancePaid,
  measurements: { chest, waist, hips, ... }
}
Response: OrderDto

PUT /api/Orders/{id}
Body: { ... }
Response: 204 No Content

DELETE /api/Orders/{id}
Response: 204 No Content
```

---

## 🎬 Demo Flow

### 1. Registration & Login Demo (2 minutes)

```
1. Open http://localhost:3001/register
2. Show the professional UI design
3. Register new user:
   - Username: demo_admin
   - Email: admin@tailoring.com
   - Password: Demo@123
   - Role: Admin
4. Explain password hashing on backend
5. Show success message
6. Navigate to login page
7. Login with credentials
8. Explain JWT token generation
9. Show dashboard loading
```

### 2. Dashboard Overview (3 minutes)

```
1. Show order statistics cards:
   - Total Orders
   - Pending Orders
   - In Progress
   - Completed
2. Explain real-time data from API
3. Show order cards with:
   - Customer name
   - Garment & fabric type
   - Status badge (color-coded)
   - Priority
   - Payment details (Total, Paid, Balance)
4. Demonstrate pagination:
   - Click through pages
   - Show ellipsis for many pages
   - Smooth scroll to top
5. Show search/filter functionality
```

### 3. Create Order Demo (5 minutes)

```
1. Click "Create New Order" button
2. Explain form sections:

   A. Customer Information
   - Enter first name: "John"
   - Leave other fields empty, blur field
   - Show validation error message
   - Fill last name: "Doe"
   - Email: john@example.com
   - Phone: +1234567890
   - Explain: Creates customer first via API

   B. Order Details
   - Garment Type: "Suit"
   - Fabric Type: "Wool"
   - Due Date: Select future date
   - Priority: High

   C. Measurements
   - Chest: 40
   - Waist: 34
   - Hips: 38
   - Explain custom measurements

   D. Payment Information
   - Total Amount: 500
   - Advance Paid: 200
   - Show automatic balance calculation: $300
   - Try to enter advance > total
   - Show validation error

3. Submit form
4. Explain API flow:
   - POST /api/customers (create customer)
   - POST /api/orders (create order with customerId)
5. Show success message
6. Redirect to dashboard
7. Show new order in list
```

### 4. Order Management (3 minutes)

```
1. Click on order card
2. Show order details page
3. Update order status:
   - Pending → In Progress
   - Explain status workflow
4. Add progress update:
   - "Measurements completed"
   - "Fabric cutting in progress"
5. Show progress timeline
6. Update payment:
   - Additional payment of $100
   - Show updated balance: $200
```

### 5. Technical Architecture (5 minutes)

```
1. Show VS Code with project structure
2. Explain Clean Architecture:
   - API Layer (Controllers)
   - Core Layer (Business Logic)
   - Infrastructure Layer (Database)
3. Show Program.cs:
   - Dependency Injection
   - JWT Configuration
   - CORS Setup
4. Show AuthService:
   - BCrypt password hashing
   - JWT token generation
5. Show OrdersController:
   - [Authorize] attribute
   - CRUD operations
6. Show Entity Framework:
   - DbContext
   - Migrations
   - Relationships
7. Frontend:
   - Redux state management
   - Axios interceptors (auto token)
   - Formik validation
   - TypeScript interfaces
```

### 6. Security Features (2 minutes)

```
1. Show password hashing in database
2. Explain JWT token structure
3. Show token in browser DevTools:
   - Application → Local Storage
4. Demonstrate protected routes:
   - Logout
   - Try to access /dashboard
   - Auto-redirect to login
5. Show API authorization:
   - Remove token from localStorage
   - Try API call → 401 Unauthorized
```

### 7. Q&A Preparation

**Expected Questions:**

Q: Why Clean Architecture?
A: Separation of concerns, testability, maintainability. Business logic independent of framework.

Q: Why JWT over sessions?
A: Stateless, scalable, works with mobile apps, microservices-friendly.

Q: Why Redux?
A: Centralized state, predictable updates, DevTools for debugging, scales with app complexity.

Q: Why TypeScript?
A: Type safety catches errors at compile time, better IDE support, self-documenting code.

Q: How do you handle concurrent updates?
A: EF Core optimistic concurrency with row versions, last-write-wins strategy.

Q: What about SQL injection?
A: EF Core uses parameterized queries, input validation on both sides, prepared statements.

Q: Scalability concerns?
A: Can add Redis for caching, load balancer for API, database replication, Azure/AWS deployment.

Q: Testing strategy?
A: Unit tests for services (xUnit), integration tests for API (TestServer), E2E tests (Playwright).

---

## 📊 Key Metrics

- **Backend:** 5 entities, 3 layers, 15+ API endpoints
- **Frontend:** 8 components, Redux state management, Type-safe with TypeScript
- **Security:** JWT auth, BCrypt hashing, CORS protection, Input validation
- **Database:** 5 tables, Relationships (1:1, 1:M), Migrations
- **Validation:** Yup schemas, Model validation, Business rules
- **UI/UX:** Professional ERP design, Responsive, Form validation feedback, Pagination

---

## 🚀 Running the Project

### Backend

```bash
cd Backend/Tailoring.API
dotnet restore
dotnet ef database update
dotnet run
```

Opens at: http://localhost:5167

### Frontend

```bash
cd Frontend
npm install
npm start
```

Opens at: http://localhost:3001

---

## 📝 Key Takeaways for Interview

1. **Clean Architecture** - Proper separation of concerns
2. **Security First** - JWT, BCrypt, validation on both sides
3. **Type Safety** - TypeScript + C# strong typing
4. **Modern Stack** - Latest .NET 9, React 19, EF Core 9
5. **Professional UI** - ERP-style design, responsive, accessible
6. **State Management** - Redux for predictable state updates
7. **Form Validation** - Formik + Yup for robust validation
8. **API Design** - RESTful, proper status codes, DTOs
9. **Database Design** - Normalized, proper relationships, migrations
10. **Developer Experience** - Well-structured, maintainable, documented

---

## 🎓 Conclusion

This is a **production-ready** tailoring management system demonstrating:

- Full-stack development skills
- Modern architecture patterns
- Security best practices
- Professional UI/UX design
- Scalable code structure
- Industry-standard tools and frameworks

**Total Development:** Professional-grade ERP system with authentication, authorization, CRUD operations, form validation, and modern design.

---

_Good luck with your demo! 🎉_
