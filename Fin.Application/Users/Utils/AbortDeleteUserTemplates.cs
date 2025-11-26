namespace Fin.Application.Users.Utils;

public static class AbortDeleteUserTemplates
{
    public const string AbortDeletionSubject = "{{appName}} - Solicitação de deleção abortada";

    public const string AbortDeletionPlainTemplate = @"
{{appName}} - Solicitação de deleção abortada

Sua solicitação de deleção do {{appName}} foi abortada.

Sua conta não será mais deletada e continua ativa.
";

    public const string AbortDeletionTemplate = @"
<!DOCTYPE html>
<html lang='pt-BR'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Solicitação de deleção abortada</title>
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

        /* Estilo de sucesso/confirmação positiva */
        .success-box {
            background: #d4edda;
            border-left: 4px solid #28a745;
            padding: 20px;
            border-radius: 6px;
            margin: 20px 0;
            text-align: left;
        }

        .success-box h3 {
            margin: 0 0 10px 0;
            color: #155724;
            font-size: 18px;
        }

        .success-box p {
            margin: 0;
            color: #155724;
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
            <h2 class='title'>Solicitação de deleção abortada</h2>
            
            <p class='message'>
                Recebemos sua confirmação para cancelar o processo de exclusão.
            </p>

            <div class='success-box'>
                <h3>Conta Segura</h3>
                <p>Sua solicitação de deleção do {{appName}} foi abortada e sua conta <strong>não será mais deletada</strong>.</p>
            </div>

            <p class='message'>
                Você pode continuar utilizando nossos serviços normalmente.
            </p>
        </div>

        <div class='footer'>
            <p>Se você não realizou esta ação, entre em contato conosco.</p>
        </div>
    </div>
</body>
</html>
";
}