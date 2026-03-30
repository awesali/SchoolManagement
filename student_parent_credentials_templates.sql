-- Student and Parent Credentials Email Templates
-- Run these SQL scripts to add the required email templates

-- 1. STUDENT_CREDENTIALS Template
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

-- 2. PARENT_CREDENTIALS Template
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

-- 3. STUDENT_LOGIN_NOTIFICATION Template (Optional)
INSERT INTO EmailTemplates (TemplateName, Subject, Body, IsActive) VALUES 
('STUDENT_LOGIN_NOTIFICATION', 
'New Login to Your {{SchoolName}} Account', 
'
<html>
<body>
    <h2>Login Notification</h2>
    
    <p>Dear {{StudentName}},</p>
    
    <p>This is to inform you that your account was accessed on {{LoginDate}} at {{LoginTime}}.</p>
    
    <div style="background-color: #f5f5f5; padding: 15px; border-radius: 5px; margin: 10px 0;">
        <p><strong>Email:</strong> {{Email}}</p>
        <p><strong>Login Date:</strong> {{LoginDate}}</p>
        <p><strong>Login Time:</strong> {{LoginTime}}</p>
        <p><strong>School:</strong> {{SchoolName}}</p>
    </div>
    
    <p>If this was not you, please contact the school administration immediately.</p>
    
    <p>Best regards,<br>
    The {{SchoolName}} Team</p>
</body>
</html>
', 1);

-- 4. PARENT_LOGIN_NOTIFICATION Template (Optional)
INSERT INTO EmailTemplates (TemplateName, Subject, Body, IsActive) VALUES 
('PARENT_LOGIN_NOTIFICATION', 
'New Login to Your {{SchoolName}} Parent Account', 
'
<html>
<body>
    <h2>Parent Portal Login Notification</h2>
    
    <p>Dear {{ParentName}},</p>
    
    <p>This is to inform you that your parent portal account was accessed on {{LoginDate}} at {{LoginTime}}.</p>
    
    <div style="background-color: #f5f5f5; padding: 15px; border-radius: 5px; margin: 10px 0;">
        <p><strong>Email:</strong> {{Email}}</p>
        <p><strong>Login Date:</strong> {{LoginDate}}</p>
        <p><strong>Login Time:</strong> {{LoginTime}}</p>
        <p><strong>School:</strong> {{SchoolName}}</p>
    </div>
    
    <p>If this was not you, please contact the school administration immediately.</p>
    
    <p>Best regards,<br>
    The {{SchoolName}} Administration Team</p>
</body>
</html>
', 1);

-- 5. PASSWORD_RESET Template (Optional)
INSERT INTO EmailTemplates (TemplateName, Subject, Body, IsActive) VALUES 
('PASSWORD_RESET', 
'{{SchoolName}} - Password Reset Request', 
'
<html>
<body>
    <h2>Password Reset Request</h2>
    
    <p>Dear {{Name}},</p>
    
    <p>We received a request to reset your password for your {{SchoolName}} account.</p>
    
    <div style="background-color: #f5f5f5; padding: 15px; border-radius: 5px; margin: 10px 0;">
        <p><strong>Email:</strong> {{Email}}</p>
        <p><strong>Role:</strong> {{RoleName}}</p>
        <p><strong>School:</strong> {{SchoolName}}</p>
    </div>
    
    <p>Your password has been reset to:</p>
    
    <div style="background-color: #e8f5e8; padding: 15px; border-radius: 5px; margin: 10px 0; text-align: center;">
        <h3 style="color: #2e7d32; margin: 0;">{{NewPassword}}</h3>
    </div>
    
    <p><strong>Important:</strong> Please change your password after login for security.</p>
    
    <p>If you did not request this password reset, please contact the school administration immediately.</p>
    
    <p>Best regards,<br>
    The {{SchoolName}} Administration Team</p>
</body>
</html>
', 1);

-- Verify the templates were added
SELECT * FROM EmailTemplates WHERE TemplateName IN 
('STUDENT_CREDENTIALS', 'PARENT_CREDENTIALS', 'STUDENT_LOGIN_NOTIFICATION', 'PARENT_LOGIN_NOTIFICATION', 'PASSWORD_RESET');
