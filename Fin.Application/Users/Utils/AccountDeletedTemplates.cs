namespace Fin.Application.Users.Utils;

public static class AccountDeletedTemplates
{
    public const string AccountDeletedSubject = "{{appName}} - Conta deletada";

    public const string AccountDeletedPlainTemplate = @"
{{appName}} - Conta deletada

Sua conta no {{appName}} foi deletada.

Agora você não poderá mais acessar seus dados e eles foram removidos da plataforma.

Agradecemos pelo tempo que passou conosco.
";

    public const string AccountDeletedTemplate = @"
<!DOCTYPE html>
<html lang='pt-BR'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Conta deletada</title>
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

        /* Estilo de 'Perigo/Remoção' para enfatizar que acabou */
        .deletion-notice {
            background: #f8d7da;
            border-left: 4px solid #dc3545;
            padding: 20px;
            border-radius: 6px;
            margin: 20px 0;
            text-align: left;
        }

        .deletion-notice h3 {
            margin: 0 0 10px 0;
            color: #721c24;
            font-size: 18px;
        }

        .deletion-notice p {
            margin: 0;
            color: #721c24;
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
            <h2 class='title'>Conta Deletada</h2>
            
            <p class='message'>
                Informamos que o processo de exclusão foi concluído.
            </p>

            <div class='deletion-notice'>
                <h3>Acesso Removido</h3>
                <p>Sua conta no {{appName}} foi deletada. Agora você não poderá mais acessar seus dados e eles foram removidos da plataforma.</p>
            </div>

            <p class='message'>
                Agradecemos pelo tempo em que esteve conosco.
            </p>
        </div>

        <div class='footer'>
            <p>Esperamos vê-lo novamente no futuro.</p>
        </div>
    </div>
</body>
</html>
";
}