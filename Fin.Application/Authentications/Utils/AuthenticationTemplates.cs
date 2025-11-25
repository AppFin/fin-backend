namespace Fin.Application.Authentications.Utils;

public static class AuthenticationTemplates
{
    public const string ExternalLoginSuccessHtml = @"
<!DOCTYPE html>
<html>
<head>
    <title>Google Authentication</title>
    <style>
        body {
            font-family: Arial, sans-serif; 
            display: flex; 
            justify-content: center; 
            align-items: center; 
            height: 100vh; 
            margin: 0;
            background: #f5f5f5;
        }
        .container {
            text-align: center; 
            background: white;
            padding: 2rem;
            border-radius: 8px;
            box-shadow: 0 2px 10px rgba(0,0,0,0.1);
        }
        .loading { color: #666; }
    </style>
</head>
<body>
    <div class='container'>
        <p class='loading'>Redirecting...</p>
    </div>
    <script>
        (function() {
            const allowedOrigins = [{{originsScript}}];
            const result = {{jsonResult}};
            
            allowedOrigins.forEach(origin => {
                try {
                    window.opener?.postMessage({
                        type: 'GOOGLE_AUTH_SUCCESS',
                        result: result
                    }, origin);
                } catch (e) {
                    console.warn('Error on post message to:', origin, e);
                }
            });
            
            setTimeout(() => {
                window.close();
            }, 1000);
        })();
    </script>
</body>
</html>
";

    public const string ExternalLoginFailHtml = @"
<!DOCTYPE html>
<html>
<head>
    <title>Error on authentication</title>
    <style>
        body { 
            font-family: Arial, sans-serif; 
            display: flex; 
            justify-content: center; 
            align-items: center; 
            height: 100vh; 
            margin: 0;
            background: #f5f5f5;
        }
        .container { 
            text-align: center; 
            background: white;
            padding: 2rem;
            border-radius: 8px;
            box-shadow: 0 2px 10px rgba(0,0,0,0.1);
        }
        .error { color: #f44336; }
    </style>
</head>
<body>
    <div class='container'>
        <h2 class='error'>Error on authentication</h2>
        <p>{{errorMessage}}</p>
        <p>This windows gonna close soon.</p>
    </div>
    <script>
        (function() {
            const allowedOrigins = [{{originsScript}}];
            
            allowedOrigins.forEach(origin => {
                try {
                    window.opener?.postMessage({
                        type: 'GOOGLE_AUTH_ERROR',
                        error: '{{errorMessage}}'
                    }, origin);
                } catch (e) {
                    console.warn('Error on post message to:', origin, e);
                }
            });
            
            setTimeout(() => {
                window.close();
            }, 3000);
        })();
    </script>
</body>
</html>
";

    public const string ResetPasswordEmailSubject = "{{appName}} - Reset Your Password";

    public const string ResetPasswordEmailPlainTemplate = @"
{{appName}} - Password Reset
We received a request to reset your password.
To create a new password, please copy and paste the link below into your browser:
{{resetLink}}
This link expires in {{linkLifeTime}} hours.
If you didn't request this, please ignore this email.
";

    public const string ResetPasswordEmailTemplate = @"
<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Reset Your Password</title>
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

        .reset-button {
            display: inline-block;
            background: linear-gradient(135deg, #f87b07 0%, #fdc570 100%);
            color: white;
            padding: 15px 30px;
            text-decoration: none;
            border-radius: 6px;
            font-weight: bold;
            margin-bottom: 30px;
        }

        .plain-link {
            background: #f8f9fa;
            padding: 15px;
            border-radius: 6px;
            margin: 20px 0;
            text-align: left;
        }

        .plain-link p {
            margin: 0 0 10px 0;
            color: #6c757d;
            font-size: 14px;
        }

        .plain-link a {
            color: #f87b07;
            word-break: break-all;
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
            <h2 class='title'>Reset Your Password</h2>
            
            <p class='message'>
                We received a request to reset your password. Click the button below to create a new password.
            </p>

            <a href='{{resetLink}}' class='reset-button'>Reset Password</a>

            <div class='plain-link'>
                <p>Or copy and paste this link:</p>
                <a href='{{resetLink}}'>{{resetLink}}</a>
            </div>
        </div>

        <div class='footer'>
            <p>This link expires in {{linkLifeTime}} hours. If you didn't request this, ignore this email.</p>
        </div>
    </div>
</body>
</html>
";
}