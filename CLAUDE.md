# KARSHA · backend (API .NET)

Instrucciones para quien trabaje en este repo (personas o agentes).

## Cómo se sube a Git y se publica en Azure

KARSHA son dos repos que se publican por separado:

| Repo | Qué es | Se publica en |
|---|---|---|
| `karsha` | Frontend Angular | Azure Static Web Apps `karsha-web-e48a85` → https://karsha.mx |
| `karsha-backend` | API .NET | Azure App Service `karsha-api-e48a85` → https://api.karsha.mx |

Ambos viven en la misma carpeta raíz, junto con los scripts de revisar y publicar (esos scripts no están en Git):

| Máquina | Carpeta raíz | Front | Back | Scripts |
|---|---|---|---|---|
| Mac | `~/Desktop/Repositorio/Tarjetas Pro` | `frontend/` | `backend/` | `check.sh`, `publicar-api.sh`, `publicar-web.sh` |
| Windows | `C:\Proyectos` | `karsha\` | `karsha-backend\` | `check.ps1`, `publicar-api.ps1`, `publicar-web.ps1` |

### Regla principal: publicar solo lo que ya está en GitHub
Se trabaja desde más de una computadora. Cada script publica lo que hay en la carpeta local; si esa copia está atrasada, regresa producción a una versión vieja y borra el trabajo de la otra máquina.

### Orden obligatorio
1. **Traer lo último** en los dos repos: `git pull`.
2. Hacer los cambios.
3. **Revisar**: desde la carpeta raíz, `./check.sh` (Mac) o `.\check.ps1` (Windows). Todo debe salir ✔.
4. **Guardar y subir**:
   ```bash
   git add .
   git commit -m "fix(editor): descripción corta en español"
   git push
   ```
   Prefijos: `feat:` algo nuevo, `fix:` arreglo, `chore:` mantenimiento. Sin línea `Co-Authored-By`.
   Si `git push` es rechazado: `git pull`, volver a revisar y otra vez `git push`.
5. **Publicar** (desde la carpeta raíz, con `az login` hecho):
   - API: `./publicar-api.sh` · `.\publicar-api.ps1`
   - Web: `./publicar-web.sh` · `.\publicar-web.ps1` (en la Mac necesita OrbStack/Docker abierto)
   - Si cambiaron los dos, primero la API y luego la web.
6. **Confirmar**: la API debe responder `HTTP 200`; la web debe decir `✔ karsha.mx ya tiene la versión nueva` (si no, esperar un minuto: Azure tarda).

### Antes de publicar, comprobar
- `git status` sin cambios pendientes y sin commits por subir (`## main...origin/main`, sin `ahead`/`behind`).
- No publicar a la vez desde dos computadoras.
- Publicar siempre con permiso del dueño del proyecto.

### Lo que publicar no toca
- Variables y secretos de Azure (base de datos, `Jwt:Key`, Stripe, correo): se cambian en el App Service, nunca en el repo.
- Datos de la base (tarjetas, usuarios, pagos). Las migraciones de la API se aplican solas al arrancar, por eso nunca se publica una API más vieja que la que está en producción.
- Secretos locales: `dotnet user-secrets` en la máquina; jamás en Git.
