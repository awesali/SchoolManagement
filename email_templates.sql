-- Email Templates for Student and Parent Notifications
-- Run these SQL scripts to add the required email templates

-- 1. STUDENT_WELCOME Template
INSERT INTO EmailTemplates (TemplateName, Subject, Body, IsActive) VALUES 
('STUDENT_WELCOME', 
'Welcome to {{SchoolName}} - {{StudentName}}!', 
'
<html>
<body>
    <h2>Welcome to {{SchoolName}}, {{StudentName}}!</h2>
    
    <p>Dear {{StudentName}},</p>
    
    <p>We are delighted to welcome you to our school family. You have been successfully enrolled in:</p>
    
    <ul>
        <li><strong>Class:</strong> {{ClassName}}</li>
        <li><strong>School:</strong> {{SchoolName}}</li>
        <li><strong>Parent Contact:</strong> {{ParentName}}</li>
    </ul>
    
    <p>Your journey with us begins today, and we''re excited to help you grow and learn.</p>
    
    <p>If you have any questions, please don''t hesitate to contact us.</p>
    
    <p>Best regards,<br>
    The {{SchoolName}} Team</p>
</body>
</html>
', 1);

-- 2. PARENT_WELCOME Template
INSERT INTO EmailTemplates (TemplateName, Subject, Body, IsActive) VALUES 
('PARENT_WELCOME', 
'Your Child {{StudentName}} is Now Enrolled at {{SchoolName}}', 
'
<html>
<body>
    <h2>Dear {{ParentName}},</h2>
    
    <p>We are pleased to inform you that your child, <strong>{{StudentName}}</strong>, has been successfully enrolled at {{SchoolName}}.</p>
    
    <p><strong>Enrollment Details:</strong></p>
    <ul>
        <li><strong>Student Name:</strong> {{StudentName}}</li>
        <li><strong>Class:</strong> {{ClassName}}</li>
        <li><strong>School:</strong> {{SchoolName}}</li>
        <li><strong>Enrollment Date:</strong> ' + CONVERT(varchar, GETDATE(), 106) + '</li>
    </ul>
    
    <p>We look forward to partnering with you in your child''s educational journey. Please keep this email for your records.</p>
    
    <p>For any queries or to schedule a meeting, please contact our school administration.</p>
    
    <p>Best regards,<br>
    The {{SchoolName}} Administration Team</p>
</body>
</html>
', 1);

-- 3. STUDENT_UPDATE Template
INSERT INTO EmailTemplates (TemplateName, Subject, Body, IsActive) VALUES 
('STUDENT_UPDATE', 
'Your Information Has Been Updated - {{SchoolName}}', 
'
<html>
<body>
    <h2>Student Information Updated</h2>
    
    <p>Dear {{StudentName}},</p>
    
    <p>This is to inform you that your information has been successfully updated in our system on {{UpdateDate}}.</p>
    
    <p><strong>Updated Details:</strong></p>
    <ul>
        <li><strong>Name:</strong> {{StudentName}}</li>
        <li><strong>Email:</strong> {{Email}}</li>
        <li><strong>School:</strong> {{SchoolName}}</li>
        <li><strong>Update Date:</strong> {{UpdateDate}}</li>
    </ul>
    
    <p>If you did not request these changes or have any questions, please contact our school administration immediately.</p>
    
    <p>Best regards,<br>
    The {{SchoolName}} Team</p>
</body>
</html>
', 1);

-- 4. PARENT_UPDATE Template
INSERT INTO EmailTemplates (TemplateName, Subject, Body, IsActive) VALUES 
('PARENT_UPDATE', 
'Your Child''s Information Has Been Updated - {{SchoolName}}', 
'
<html>
<body>
    <h2>Child Information Updated</h2>
    
    <p>Dear {{ParentName}},</p>
    
    <p>This is to inform you that your child <strong>{{StudentName}}</strong>''s information has been successfully updated in our system on {{UpdateDate}}.</p>
    
    <p><strong>Updated Details:</strong></p>
    <ul>
        <li><strong>Student Name:</strong> {{StudentName}}</li>
        <li><strong>Parent Email:</strong> {{Email}}</li>
        <li><strong>School:</strong> {{SchoolName}}</li>
        <li><strong>Update Date:</strong> {{UpdateDate}}</li>
    </ul>
    
    <p>If you did not request these changes or have any questions about your child''s information, please contact our school administration.</p>
    
    <p>Best regards,<br>
    The {{SchoolName}} Administration Team</p>
</body>
</html>
', 1);

-- 5. Optional: STUDENT_ADMISSION_CONFIRMATION Template
INSERT INTO EmailTemplates (TemplateName, Subject, Body, IsActive) VALUES 
('STUDENT_ADMISSION_CONFIRMATION', 
'Admission Confirmation - {{SchoolName}}', 
'
<html>
<body>
    <h2>Admission Confirmation</h2>
    
    <p>Dear {{StudentName}} and {{ParentName}},</p>
    
    <p>We are pleased to confirm the successful admission of {{StudentName}} to {{SchoolName}}.</p>
    
    <p><strong>Admission Confirmation:</strong></p>
    <ul>
        <li><strong>Student Name:</strong> {{StudentName}}</li>
        <li><strong>Class:</strong> {{ClassName}}</li>
        <li><strong>Parent Name:</strong> {{ParentName}}</li>
        <li><strong>Parent Email:</strong> {{ParentEmail}}</li>
        <li><strong>School:</strong> {{SchoolName}}</li>
        <li><strong>Academic Year:</strong> {{AcademicYear}}</li>
    </ul>
    
    <p>Please keep this confirmation for your records. The school will contact you soon with further information about the academic calendar, school supplies, and orientation.</p>
    
    <p>Welcome to the {{SchoolName}} family!</p>
    
    <p>Best regards,<br>
    The {{SchoolName}} Admission Team</p>
</body>
</html>
', 1);

-- Verify the templates were added
SELECT * FROM EmailTemplates WHERE TemplateName IN 
('STUDENT_WELCOME', 'PARENT_WELCOME', 'STUDENT_UPDATE', 'PARENT_UPDATE', 'STUDENT_ADMISSION_CONFIRMATION');
