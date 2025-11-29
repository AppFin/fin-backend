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
}