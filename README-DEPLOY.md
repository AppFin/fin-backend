# Deploy da portfolio-demo-mode

Todo push em `portfolio-demo-mode` (neste repo) dispara `.github/workflows/deploy-portfolio-demo.yml`,
que entra no servidor via SSH, clona/atualiza este repo e o `fin-frontend` na mesma branch,
builda as duas imagens Docker e sobe com `docker-compose.prod.yml`.

## O que falta configurar

**1. Secrets no GitHub** (Settings > Secrets and variables > Actions, neste repo):
- `DEMO_SSH_HOST` - IP/host do servidor ARM
- `DEMO_SSH_USER` - usuário SSH
- `DEMO_SSH_KEY` - chave privada SSH (a pública precisa estar no `authorized_keys` do servidor)

**2. No servidor**, uma vez:
```bash
docker network create web   # rede externa que o proxy reverso vai usar depois
mkdir -p ~/fin-portfolio-demo/fin-backend
cp .env.example ~/fin-portfolio-demo/fin-backend/.env   # depois de clonar, ou copie o conteúdo abaixo
```
Preencha `~/fin-portfolio-demo/fin-backend/.env` (nunca vai pro git) com base no `.env.example` deste repo.
Os valores de `Encrypt__Key`/`Encrypt__Iv`/`Jwt__Key` têm que ser reais - com placeholder tipo
`EXEMPLE` a API quebra ao tentar cadastrar usuário (a criptografia exige 32/16 caracteres exatos).

**3. Proxy/domínio**: por enquanto os containers `fin-api` e `fin-front` só entram na rede
`web`, sem porta publicada no host. Quando você configurar o proxy reverso, aponte para os
container names (`fin-api:8080`, `fin-front:80`) nessa rede. Depois de ter domínios reais,
atualize `PUBLIC_API_URL` e `ApiSettings__FrontendConfigs__Url` no `.env` do servidor e rode
o workflow de novo (ou `workflow_dispatch` manual) para rebuildar o front com a URL certa.

## Rodando manualmente (sem esperar o CI)

```bash
cd ~/fin-portfolio-demo
git clone -b portfolio-demo-mode https://github.com/AppFin/fin-backend.git fin-backend
git clone -b portfolio-demo-mode https://github.com/AppFin/fin-frontend.git fin-frontend
cp fin-backend/.env.example fin-backend/.env   # e preencha
set -a; source fin-backend/.env; set +a
docker build --memory=1g --memory-swap=1g -t fin-api:latest -f fin-backend/Dockerfile fin-backend
docker build --memory=1g --memory-swap=1g --build-arg API_URL="$PUBLIC_API_URL" -t fin-front:latest fin-frontend
cd fin-backend && docker compose -f docker-compose.prod.yml up -d
```

## Limites de recursos aplicados

- `docker build` com `--memory=1g` em cada imagem (backend e front, um de cada vez, nunca em paralelo).
- Backend: GC do .NET em modo workstation (`DOTNET_gcServer=0`), bem mais leve que o server GC padrão.
- Frontend: build do Angular limitado a 1GB de heap (`NODE_OPTIONS=--max-old-space-size=1024`).
- Ajuste os valores de `--memory` no workflow/comandos acima conforme o RAM real da VM.
