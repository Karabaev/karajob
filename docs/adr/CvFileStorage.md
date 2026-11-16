# CvFileStorage

## Status
Accepted. Implemented for issue #11.

## Context
An application references a specific CV version, so the uploaded PDF/DOCX has to be stored and served back under its original name. design.md puts the files on the `/data/files` volume, with the path in the DB. Blazor Server limits `IBrowserFile.OpenReadStream` to 500 KB by default.

## Related ADRs
- [[ApplicationDbContextAbstraction]] — the same technique: the interface in Application, the implementation in Infrastructure.

## Decisions

### 1. Files on disk, a relative path in the DB
**Decision.** `Karajob.Application.Files.IFileStorage` (`SaveAsync` / `OpenRead` / `Delete`) works with relative paths using `/`; `CvVersion.StoragePath` stores exactly that (`cv/{userId}/{guid}.pdf`). `LocalFileStorage` in Infrastructure resolves the path from `Files:RootDirectory` (`/data/files` in the container, `data/files` from the content root in Development) and rejects paths outside the root. The on-disk file name does not depend on the name sent by the client; the original name lives in `CvVersion.FileName`.

**Alternatives.**
- A BLOB in SQLite — bloats the database and its backup on every migration, and complicates a move to PostgreSQL.
- An absolute path in the DB — breaks when the mount point changes.

### 2. Write order: file → DB, deletion order: DB → file
**Decision.** On upload the file is written first, then `SaveChanges`; if the DB fails, the file is deleted. On deletion the DB is committed first, then the files are deleted. In the worst case a stray file remains, but never a record pointing to a missing file.

**Alternatives.**
- A transaction spanning the file system — there is no portable mechanism, and the gain is small for a single-user installation.

### 3. Download is a Minimal API, not JS interop
**Decision.** `GET /cvs/versions/{id}/file` calls `CvService.OpenVersionAsync` and returns `Results.File` with `fileDownloadName`. In the UI it is a plain link with the `download` attribute: the Blazor router does not intercept such links, and the file goes as an ordinary HTTP response rather than through the SignalR circuit. The `UserId` filter applies here too: someone else's version is a 404.

**Alternatives.**
- Streaming via `IJSRuntime` (`DotNetStreamReference`) — extra JS and the whole load on the websocket.

### 4. The size limit comes from config, validation in the service
**Decision.** `Cv:MaxFileSizeMb` (5) is bound to `CvOptions`. Web passes it to `OpenReadStream(maxAllowedSize)`, and `CvService` checks the size and the extension (`.pdf` / `.docx`) before opening the stream: `CvUpload.OpenRead` is a delegate. The Content-Type is derived from the extension; the browser-supplied one is not trusted.
