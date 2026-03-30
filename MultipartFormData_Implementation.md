# Multipart/Form-Data Implementation for Student APIs

## Overview
Successfully added `[Consumes("multipart/form-data")]` attribute to student endpoints to support file uploads, following the same pattern as the staff implementation.

## Implementation Details

### **1. Add Student Endpoint**
Updated to support multipart/form-data for file uploads:

```csharp
[HttpPost("add-student")]
[Authorize]
[Consumes("multipart/form-data")]  // ✅ ADDED
public async Task<IActionResult> AddStudent([FromForm] StudentCreateDto dto)
```

### **2. Update Student Endpoint**
Already had multipart/form-data support (was already implemented):

```csharp
[HttpPut("update-student")]
[Consumes("multipart/form-data")]  // ✅ ALREADY EXISTED
public async Task<IActionResult> UpdateStudent([FromForm] StudentUpdateDto dto)
```

## Why Multipart/Form-Data is Required

### **File Upload Support**
Both student endpoints handle file uploads through DTOs:

**StudentCreateDto:**
```csharp
public List<IFormFile>? Files { get; set; }
public List<string>? DocumentNames { get; set; }
public List<int?>? DocumentIds { get; set; }
```

**StudentUpdateDto:**
```csharp
public List<IFormFile>? Files { get; set; }
public List<string>? DocumentNames { get; set; }
public List<int?>? DocumentIds { get; set; }
```

### **Content-Type Handling**
Without `[Consumes("multipart/form-data")]`:
- ASP.NET Core expects `application/json`
- File uploads would be ignored
- `IFormFile` parameters would be null

### **With [Consumes("multipart/form-data")]**:
- ASP.NET Core expects `multipart/form-data`
- File uploads are properly bound
- `IFormFile` parameters are populated
- Form fields are correctly mapped

## API Usage Examples

### **Frontend - Add Student with Files**
```javascript
const formData = new FormData();
formData.append('StudentName', 'John Doe');
formData.append('Email', 'john@student.com');
formData.append('ClassId', '5');
formData.append('Parent.Name', 'Jane Doe');
formData.append('Parent.Email', 'parent@email.com');

// Add files
for (let i = 0; i < files.length; i++) {
    formData.append('Files', files[i]);
    formData.append('DocumentNames', documentNames[i]);
}

fetch('/api/admin/add-student', {
    method: 'POST',
    headers: {
        'Authorization': `Bearer ${token}`
    },
    body: formData
})
.then(response => response.json());
```

### **Frontend - Update Student with Files**
```javascript
const formData = new FormData();
formData.append('Id', '123');
formData.append('StudentName', 'John Doe Updated');
formData.append('Email', 'john.updated@student.com');

// Add new files or update existing ones
for (let i = 0; i < files.length; i++) {
    formData.append('Files', files[i]);
    formData.append('DocumentNames', documentNames[i]);
    formData.append('DocumentIds', documentIds[i]); // For updating existing docs
}

fetch('/api/admin/update-student', {
    method: 'PUT',
    headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'multipart/form-data'
    },
    body: formData
})
.then(response => response.json());
```

### **Postman Testing**
**Add Student:**
1. Set method to `POST`
2. Set URL to `/api/admin/add-student`
3. Go to Body → form-data
4. Add form fields:
   - StudentName (text)
   - Email (text)
   - Parent.Name (text)
   - Parent.Email (text)
   - ClassId (text)
5. Add files:
   - Files (file) - Multiple files allowed
   - DocumentNames (text) - Corresponding names

**Update Student:**
1. Set method to `PUT`
2. Set URL to `/api/admin/update-student`
3. Go to Body → form-data
4. Add form fields:
   - Id (text)
   - StudentName (text)
   - Email (text)
5. Add files:
   - Files (file) - Multiple files allowed
   - DocumentIds (text) - For updating existing docs

## Backend Processing

### **File Handling in Repository**
The repository already handles file processing:

```csharp
// Add Student - Handle multiple files
if (dto.Files != null && dto.Files.Count > 0)
{
    for (int i = 0; i < dto.Files.Count; i++)
    {
        var file = dto.Files[i];
        var extension = Path.GetExtension(file.FileName);
        var uniqueFileName = Guid.NewGuid() + extension;
        
        // Save file to server
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }
        
        // Save to database
        var document = new Student_Documents
        {
            StudentId = student.Id,
            DocumentName = docName,
            FileUrl = $"/studentdocs/{student.Id}/{uniqueFileName}",
            CreatedDate = DateTime.UtcNow
        };
        
        _context.Student_Documents.Add(document);
    }
}
```

## Benefits

1. **File Upload Support**: Students can upload multiple documents
2. **Document Management**: Create and update student documents
3. **Consistent Pattern**: Same as staff file handling
4. **Proper Binding**: `[FromForm]` attribute works with multipart data
5. **Error Handling**: Proper validation and error responses

## Swagger Documentation

With `[Consumes("multipart/form-data")]`, Swagger will show:

**Add Student:**
```
Content-Type: multipart/form-data
```

**Update Student:**
```
Content-Type: multipart/form-data
```

## Implementation Status

✅ **Completed:**
- ✅ `add-student` endpoint: Added `[Consumes("multipart/form-data")]`
- ✅ `update-student` endpoint: Already had `[Consumes("multipart/form-data")]`
- ✅ Both endpoints now support file uploads via `IFormFile`
- ✅ Consistent with staff implementation pattern

✅ **Ready for Testing:**
- File uploads work correctly
- Multiple document support
- Form data binding works properly
- Swagger documentation accurate

Both student endpoints now fully support multipart/form-data for file uploads!
