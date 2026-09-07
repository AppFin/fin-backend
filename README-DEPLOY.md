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
mkdir -p ~/fin-portfolio-demo/fin-backend
cp .env.example ~/fin-portfolio-demo/fin-backend/.env   # depois de clonar, ou copie o conteúdo abaixo
```
Preencha `~/fin-portfolio-demo/fin-backend/.env` (nunca vai pro git) com base no `.env.example` deste repo.
Os valores de `Encrypt__Key`/`Encrypt__Iv`/`Jwt__Key` têm que ser reais - com placeholder tipo
`EXEMPLE` a API quebra ao tentar cadastrar usuário (a criptografia exige 32/16 caracteres exatos).

**3. Proxy/domínio (setup ARM + AMD)**: como o proxy (NPM) roda numa VM diferente (AMD) da
que sobe estes containers (ARM), uma rede docker não ajuda - ela não atravessa hosts. Por isso
`fin-api` e `fin-front` publicam porta no host da VM ARM (`FIN_API_PORT`/`FIN_FRONT_PORT` no
`.env`, padrão `8091`/`8090`), do mesmo jeito que o n8n já faz. No NPM, aponte os "Proxy Hosts"
para o **IP privado da VM ARM** (estável mesmo quando o IP público muda) nessas portas:
- `fin-api` -> `http://<IP_PRIVADO_ARM>:8091`
- `fin-front` -> `http://<IP_PRIVADO_ARM>:8090`

Garanta que a Security List/NSG da VM ARM libera essas portas só pra sub-rede privada (não pro
`0.0.0.0/0`), já que quem deve acessá-las é só a VM AMD, nunca a internet direto.

Depois de ter os domínios reais, atualize `PUBLIC_API_URL` e `ApiSettings__FrontendConfigs__Url`
no `.env` do servidor e rode o workflow de novo (ou `workflow_dispatch` manual) para rebuildar
o front com a URL certa.

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
