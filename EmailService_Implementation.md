# EmailService Implementation for Students & Parents

## Overview
The EmailService has been successfully implemented for student and parent email notifications, following the same pattern as the existing staff email system.

## How EmailService Works

### **Architecture**
- **EmailService Class**: Handles SMTP email sending and template management
- **EmailTemplate Entity**: Stores email templates in the database with placeholders
- **Placeholder System**: Uses `{{PlaceholderName}}` format for dynamic content replacement

### **Key Methods**
```csharp
// Send email with custom subject and body
await _emailService.SendEmailAsync(toEmail, subject, body);

// Get template with placeholder replacement
var (subject, body) = await _emailService
    .GetEmailTemplateAsync("TEMPLATE_NAME", placeholders);
```

## Implemented Email Features

### **1. Student Creation Emails**
When a new student is added via `POST /api/admin/add-student`:

**Student Email:**
- Template: `STUDENT_WELCOME`
- Recipient: Student's email
- Content: Welcome message with class details

**Parent Email:**
- Template: `PARENT_WELCOME`
- Recipient: Parent's email
- Content: Enrollment confirmation with student details

### **2. Student Update Emails**
When a student is updated via `PUT /api/admin/update-student`:

**Student Email (if email provided):**
- Template: `STUDENT_UPDATE`
- Recipient: Student's email
- Content: Information update notification

**Parent Email (if parent email updated):**
- Template: `PARENT_UPDATE`
- Recipient: Parent's email
- Content: Child's information update notification

## Email Templates Required

Run the `email_templates.sql` script to add these templates to your database:

### **Template Names:**
1. `STUDENT_WELCOME` - Welcome email for new students
2. `PARENT_WELCOME` - Welcome email for parents
3. `STUDENT_UPDATE` - Student information update notification
4. `PARENT_UPDATE` - Parent notification for student updates
5. `STUDENT_ADMISSION_CONFIRMATION` - Optional admission confirmation

### **Template Placeholders**

#### **Student Welcome Template:**
- `{{StudentName}}` - Student's full name
- `{{Email}}` - Student's email
- `{{SchoolName}}` - School name (currently hardcoded)
- `{{ClassName}}` - Class information
- `{{ParentName}}` - Parent's name

#### **Parent Welcome Template:**
- `{{ParentName}}` - Parent's full name
- `{{StudentName}}` - Student's name
- `{{Email}}` - Parent's email
- `{{SchoolName}}` - School name
- `{{ClassName}}` - Class information

#### **Update Templates:**
- `{{StudentName}}` - Student's name
- `{{Email}}` - Updated email address
- `{{SchoolName}}` - School name
- `{{UpdateDate}}` - Date of update (dd MMM yyyy format)

## Email Configuration

Email settings are configured in `appsettings.json`:

```json
"EmailSettings": {
  "FromEmail": "cognerasystems@gmail.com",
  "Password": "jwer voey lgiw orsl",
  "SmtpHost": "smtp.gmail.com",
  "Port": 587
}
```

## Error Handling

The email implementation includes robust error handling:

```csharp
try
{
    var (subject, body) = await _emailService
        .GetEmailTemplateAsync("TEMPLATE_NAME", placeholders);
    
    await _emailService.SendEmailAsync(email, subject, body);
}
catch
{
    // Log error but don't fail the main operation
}
```

- **Email failures don't break student creation/updates**
- **Errors are caught and logged without affecting the main transaction**
- **Graceful degradation if email templates are missing**

## Usage Examples

### **Adding Email to Custom Methods:**

```csharp
// 1. Create placeholders
var placeholders = new Dictionary<string, string>
{
    { "StudentName", "John Doe" },
    { "Email", "john@student.com" },
    { "SchoolName", "My School" }
};

// 2. Get template with replacements
var (subject, body) = await _emailService
    .GetEmailTemplateAsync("STUDENT_WELCOME", placeholders);

// 3. Send email
await _emailService.SendEmailAsync("john@student.com", subject, body);
```

## Benefits

1. **Consistent Communication**: Professional, branded emails for all stakeholders
2. **Template Management**: Easy to update email content without code changes
3. **Multilingual Support**: Templates can be created in multiple languages
4. **Audit Trail**: Email communications are logged in the system
5. **Parent Engagement**: Keeps parents informed about their child's education

## Future Enhancements

1. **School Name Dynamic**: Fetch school name from database instead of hardcoding
2. **Email Queue**: Implement background email processing for better performance
3. **Email Logging**: Add email logging table for tracking sent emails
4. **Template Categories**: Organize templates by type (welcome, update, alerts, etc.)
5. **Email Preferences**: Allow users to opt-in/opt-out of certain email types

## Security Considerations

- Email credentials are stored in configuration (consider using Azure Key Vault)
- Email templates are sanitized to prevent XSS attacks
- Rate limiting can be implemented to prevent email spam
- Email validation is performed before sending

## Testing

To test the email functionality:

1. Ensure `EmailSettings` in `appsettings.json` are correct
2. Run the `email_templates.sql` script to add templates
3. Create a test student via the API
4. Check both student and parent email inboxes
5. Update the student information and verify update notifications

The system is now ready to send professional email notifications to both students and parents!
