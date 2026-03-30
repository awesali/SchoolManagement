# Student & Parent Credentials Implementation

## Overview
Successfully implemented student and parent credentials system following the exact same pattern as staff credentials, including database model, repository, authentication, and email notifications.

## Implementation Details

### **1. Database Model - StudentsParentsCreds**
Created model matching the provided schema:

```csharp
public class StudentsParentsCreds
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(150)]
    public string Name { get; set; }

    [Required]
    [MaxLength(255)]
    public string Email { get; set; }

    [MaxLength(20)]
    public string Phone { get; set; }

    [Required]
    [MaxLength(255)]
    public string Password_Hash { get; set; }

    [Required]
    [MaxLength(50)]
    public string RoleName { get; set; }

    [Required]
    public int School_Id { get; set; }

    public DateTime? Last_Login { get; set; }

    [MaxLength(50)]
    public string Status { get; set; }

    public DateTime Created_At { get; set; } = DateTime.Now;

    [Required]
    public bool IsActive { get; set; } = true;
}
```

### **2. DTOs - StudentParentRegisterDto & LoginDto**
Created DTOs for registration and login:

```csharp
public class StudentParentRegisterDto
{
    [Required]
    [MaxLength(150)]
    public string Name { get; set; }

    [Required]
    [EmailAddress]
    [MaxLength(255)]
    public string Email { get; set; }

    [MaxLength(20)]
    public string Phone { get; set; }

    [Required]
    public string Password { get; set; }

    [Required]
    [MaxLength(50)]
    public string RoleName { get; set; }

    [Required]
    public int School_Id { get; set; }

    [MaxLength(50)]
    public string Status { get; set; }
}

public class StudentParentLoginDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; }

    [Required]
    public string Password { get; set; }
}
```

### **3. Repository - StudentParentRepository**
Implemented full CRUD and authentication:

```csharp
public class StudentParentRepository : IStudentParentRepository
{
    // Register new student/parent
    Task<bool> RegisterStudentParentAsync(StudentParentRegisterDto dto);

    // Login with JWT token generation
    Task<string> LoginStudentParentAsync(StudentParentLoginDto dto);

    // Get user by email
    Task<StudentsParentsCreds> GetByEmailAsync(string email);

    // Update last login time
    Task<bool> UpdateLastLoginAsync(int id);
}
```

### **4. JWT Service Enhancement**
Added overloaded method for student/parent tokens:

```csharp
// Original method for staff
public string GenerateToken(Users user) { ... }

// New method for students/parents
public string GenerateToken(string email, string roleName, int userId) { ... }
```

### **5. Controller - StudentParentAuthController**
Created authentication endpoints:

```csharp
[ApiController]
[Route("api/[controller]")]
public class StudentParentAuthController : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] StudentParentRegisterDto dto)

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] StudentParentLoginDto dto)

    [HttpGet("profile")]
    [Authorize]
    public async Task<IActionResult> GetProfile()
}
```

### **6. Enhanced AdminRepository**
Updated `AddStudentAsync` to create credentials:

```csharp
// Generate passwords
var studentPassword = _common.GeneratePassword(dto.StudentName, dto.DOB);
var parentPassword = _common.GeneratePassword(dto.Parent.Name, dto.DOB);

// Create student credential
var studentCred = new StudentsParentsCreds
{
    Name = dto.StudentName,
    Email = dto.Email,
    Password_Hash = BCrypt.Net.BCrypt.HashPassword(studentPassword),
    RoleName = "Student",
    School_Id = dto.SchoolId,
    // ... other fields
};

// Create parent credential
var parentCred = new StudentsParentsCreds
{
    Name = dto.Parent.Name,
    Email = dto.Parent.Email,
    Password_Hash = BCrypt.Net.BCrypt.HashPassword(parentPassword),
    RoleName = "Parent",
    School_Id = dto.SchoolId,
    // ... other fields
};
```

### **7. Email Templates**
Updated to send credentials:

```csharp
// Student credentials email
var studentPlaceholders = new Dictionary<string, string>
{
    { "StudentName", dto.StudentName },
    { "Email", dto.Email },
    { "Password", studentPassword }, // ✅ INCLUDE PASSWORD
    { "SchoolName", "Blue Berry School" },
    { "ClassName", $"Class {dto.ClassId}" },
    { "ParentName", dto.Parent.Name }
};

// Parent credentials email
var parentPlaceholders = new Dictionary<string, string>
{
    { "ParentName", dto.Parent.Name },
    { "StudentName", dto.StudentName },
    { "Email", dto.Parent.Email },
    { "Password", parentPassword }, // ✅ INCLUDE PASSWORD
    { "SchoolName", "Blue Berry School" },
    { "ClassName", $"Class {dto.ClassId}" }
};
```

## API Endpoints

### **Student/Parent Authentication**
```
POST /api/StudentParentAuth/register
POST /api/StudentParentAuth/login
GET  /api/StudentParentAuth/profile (requires JWT)
```

### **Registration Request**
```json
{
  "name": "John Doe",
  "email": "john@student.com",
  "phone": "1234567890",
  "password": "John@01012010",
  "roleName": "Student",
  "school_Id": 1,
  "status": "Active"
}
```

### **Login Request**
```json
{
  "email": "john@student.com",
  "password": "John@01012010"
}
```

### **Login Response**
```json
{
  "success": true,
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "message": "Login successful"
}
```

### **Profile Response**
```json
{
  "success": true,
  "data": {
    "id": 1,
    "name": "John Doe",
    "email": "john@student.com",
    "phone": "1234567890",
    "roleName": "Student",
    "schoolId": 1,
    "status": "Active",
    "lastLogin": "2024-03-30T11:58:00",
    "createdAt": "2024-03-30T10:30:00",
    "isActive": true
  }
}
```

## Database Schema

### **Students_Parents_Creds Table**
```sql
CREATE TABLE [dbo].[Students_Parents_Creds] (
    [Id] INT IDENTITY(1,1) PRIMARY KEY,
    [Name] NVARCHAR(150) NOT NULL,
    [Email] NVARCHAR(255) NOT NULL UNIQUE,
    [Phone] NVARCHAR(20) NULL,
    [Password_Hash] NVARCHAR(255) NOT NULL,
    [RoleName] NVARCHAR(50) NOT NULL,
    [School_Id] INT NOT NULL,
    [Last_Login] DATETIME NULL,
    [Status] NVARCHAR(50) NULL,
    [Created_At] DATETIME NOT NULL DEFAULT GETDATE(),
    [IsActive] BIT NOT NULL DEFAULT 1
);
```

## Password Generation Pattern

Following the same pattern as staff:
- **Student Password**: `{FirstName}@{ddMMyyyy}` (DOB format)
- **Parent Password**: `{FirstName}@{ddMMyyyy}` (Student's DOB)

**Examples:**
- Student: `John@01012010` (John + @ + DOB 01/01/2010)
- Parent: `Jane@01012010` (Jane + @ + Student's DOB 01/01/2010)

## Email Templates Required

### **STUDENT_CREDENTIALS Template**
```sql
INSERT INTO EmailTemplates (TemplateName, Subject, Body, IsActive) VALUES 
('STUDENT_CREDENTIALS', 
'Welcome to {{SchoolName}} - Your Login Credentials', 
'
<html>
<body>
    <h2>Welcome to {{SchoolName}}, {{StudentName}}!</h2>
    
    <p>Dear {{StudentName}},</p>
    
    <p>Your account has been created successfully. Here are your login credentials:</p>
    
    <div style="background-color: #f5f5f5; padding: 15px; border-radius: 5px; margin: 10px 0;">
        <p><strong>Email:</strong> {{Email}}</p>
        <p><strong>Password:</strong> {{Password}}</p>
        <p><strong>School:</strong> {{SchoolName}}</p>
        <p><strong>Class:</strong> {{ClassName}}</p>
    </div>
    
    <p><strong>Important:</strong> Please change your password after first login for security.</p>
    
    <p>You can login using these credentials at our school portal.</p>
    
    <p>Best regards,<br>
    The {{SchoolName}} Team</p>
</body>
</html>
', 1);
```

### **PARENT_CREDENTIALS Template**
```sql
INSERT INTO EmailTemplates (TemplateName, Subject, Body, IsActive) VALUES 
('PARENT_CREDENTIALS', 
'Your Child {{StudentName}} is Enrolled at {{SchoolName}} - Your Login Credentials', 
'
<html>
<body>
    <h2>Dear {{ParentName}},</h2>
    
    <p>We are pleased to inform you that your child <strong>{{StudentName}}</strong> has been successfully enrolled at {{SchoolName}}.</p>
    
    <p>Here are your parent portal login credentials:</p>
    
    <div style="background-color: #f5f5f5; padding: 15px; border-radius: 5px; margin: 10px 0;">
        <p><strong>Your Email:</strong> {{Email}}</p>
        <p><strong>Your Password:</strong> {{Password}}</p>
        <p><strong>Child Name:</strong> {{StudentName}}</p>
        <p><strong>School:</strong> {{SchoolName}}</p>
        <p><strong>Class:</strong> {{ClassName}}</p>
    </div>
    
    <p><strong>Important:</strong> Please change your password after first login for security.</p>
    
    <p>Through the parent portal, you can:</p>
    <ul>
        <li>View your child''s academic progress</li>
        <li>Check attendance records</li>
        <li>Communicate with teachers</li>
        <li>Pay school fees online</li>
        <li>View school notices and events</li>
    </ul>
    
    <p>Best regards,<br>
    The {{SchoolName}} Administration Team</p>
</body>
</html>
', 1);
```

## Comparison with Staff Implementation

| Feature | Staff | Student/Parent |
|---------|--------|----------------|
| Model | `Users` | `StudentsParentsCreds` |
| Password Generation | ✅ `Name@ddMMyyyy` | ✅ `Name@ddMMyyyy` |
| Email Templates | ✅ `STAFF_CREDENTIALS` | ✅ `STUDENT_CREDENTIALS`, `PARENT_CREDENTIALS` |
| JWT Token | ✅ `GenerateToken(Users)` | ✅ `GenerateToken(email, role, id)` |
| Registration | ✅ `RegisterDto` | ✅ `StudentParentRegisterDto` |
| Login | ✅ `LoginDto` | ✅ `StudentParentLoginDto` |
| Auto-Creation | ✅ When adding staff | ✅ When adding students |

## Usage Examples

### **Frontend - Student Registration**
```javascript
fetch('/api/StudentParentAuth/register', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
        name: 'John Doe',
        email: 'john@student.com',
        password: 'John@01012010',
        roleName: 'Student',
        school_Id: 1,
        status: 'Active'
    })
})
.then(response => response.json());
```

### **Frontend - Login**
```javascript
fetch('/api/StudentParentAuth/login', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
        email: 'john@student.com',
        password: 'John@01012010'
    })
})
.then(response => response.json())
.then(data => {
    if (data.success) {
        localStorage.setItem('token', data.token);
        // Redirect to dashboard
    }
});
```

### **Frontend - Profile Access**
```javascript
fetch('/api/StudentParentAuth/profile', {
    headers: {
        'Authorization': `Bearer ${localStorage.getItem('token')}`
    }
})
.then(response => response.json());
```

## Benefits

1. **Consistent Pattern**: Same as staff implementation
2. **Security**: Password hashing with BCrypt
3. **JWT Authentication**: Token-based security
4. **Email Integration**: Automatic credential delivery
5. **Role-Based**: Separate Student and Parent roles
6. **Profile Management**: User profile access
7. **Audit Trail**: Last login tracking
8. **Soft Delete**: IsActive field for account management

## Implementation Status

✅ **Completed:**
- ✅ `StudentsParentsCreds` model
- ✅ `StudentParentRegisterDto` and `StudentParentLoginDto`
- ✅ `IStudentParentRepository` interface
- ✅ `StudentParentRepository` implementation
- ✅ `StudentParentAuthController` endpoints
- ✅ JWT service enhancement
- ✅ AdminRepository integration
- ✅ Email template updates
- ✅ Dependency injection setup

✅ **Ready for Testing:**
- ✅ `POST /api/StudentParentAuth/register`
- ✅ `POST /api/StudentParentAuth/login`
- ✅ `GET /api/StudentParentAuth/profile`
- ✅ Automatic credential creation when adding students
- ✅ Email notifications with passwords

The student and parent credentials system is now fully implemented and follows the exact same pattern as staff credentials!
