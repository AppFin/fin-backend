namespace Fin.Application.Users.Utils;

public static class CreateUserTemplates
{
    public const string SendConfirmationCodeTemplate = @"
<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Confirm Your Email</title>
    <style>
        body {
            font-family: Arial, sans-serif;
            background-color: #f8f9fa;
            color: #212529;
            margin: 0;
            padding: 20px;
        }

        .container {
            max-width: 600px;
            margin: 0 auto;
            background: white;
            border-radius: 8px;
            box-shadow: 0 2px 10px rgba(0,0,0,0.1);
        }

        .header {
            background: linear-gradient(135deg, #f87b07 0%, #fdc570 100%);
            padding: 30px;
            text-align: center;
            border-radius: 8px 8px 0 0;
        }

        .app-icon {
            width: 60px;
            height: 60px;
            background: white;
            border-radius: 8px;
            display: inline-flex;
            align-items: center;
            justify-content: center;
            margin-bottom: 15px;
        }

        .app-icon img {
            width: 40px;
            height: 40px;
        }

        .app-name {
            color: white;
            font-size: 24px;
            font-weight: bold;
            margin: 0;
        }

        .content {
            padding: 40px 30px;
            text-align: center;
        }

        .title {
            font-size: 22px;
            color: rgb(46, 38, 26);
            margin-bottom: 20px;
        }

        .message {
            color: #6c757d;
            margin-bottom: 30px;
            line-height: 1.5;
        }

        .confirmation-code {
            display: inline-block;
            background: linear-gradient(135deg, #f87b07 0%, #fdc570 100%);
            color: white;
            padding: 20px 40px;
            border-radius: 8px;
            font-size: 32px;
            font-weight: bold;
            letter-spacing: 4px;
            margin: 20px 0;
            font-family: 'Courier New', monospace;
            box-shadow: 0 4px 15px rgba(248, 123, 7, 0.3);
        }

        .code-info {
            background: #f8f9fa;
            padding: 20px;
            border-radius: 6px;
            margin: 20px 0;
            border-left: 4px solid #f87b07;
        }

        .code-info p {
            margin: 0;
            color: #6c757d;
            font-size: 14px;
            text-align: left;
        }

        .security-note {
            background: #fff3cd;
            border: 1px solid #ffeaa7;
            border-radius: 6px;
            padding: 15px;
            margin: 20px 0;
        }

        .security-note p {
            margin: 0;
            color: #856404;
            font-size: 14px;
        }

        .footer {
            background: rgb(46, 38, 26);
            color: #fdc570;
            padding: 20px 30px;
            text-align: center;
            border-radius: 0 0 8px 8px;
            font-size: 14px;
        }
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <div class='app-icon'>
                <img src='{{logoIconUrl}}' alt='{{appName}} logo'>
            </div>
            <h1 class='app-name'>{{appName}}</h1>
        </div>

        <div class='content'>
            <h2 class='title'>Confirm Your Email</h2>
            
            <p class='message'>
                To complete your registration, please use the confirmation code below:
            </p>

            <div class='confirmation-code'>
                {{confirmationCode}}
            </div>

            <div class='code-info'>
                <p><strong>How to use:</strong></p>
                <p>1. Return to the app or website</p>
                <p>2. Enter the 6-digit code in the requested field</p>
                <p>3. Click ""Confirm"" to activate your account</p>
            </div>

            <div class='security-note'>
                <p><strong>Important:</strong> This code expires in 10 minutes for security reasons. If you didn't request this code, please ignore this email.</p>
            </div>
        </div>

        <div class='footer'>
            <p>If you're unable to confirm your email, please contact our support team.</p>
            <p>This is an automated email, please do not reply to this message.</p>
        </div>
    </div>
</body>
</html>
";
}