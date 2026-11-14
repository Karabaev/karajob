# CvFileStorage

## Status
Accepted. Implemented for issue #11.

## Context
Отклик ссылается на конкретную версию CV, поэтому загруженный PDF/DOCX нужно хранить и отдавать обратно с исходным именем. design.md кладёт файлы на volume `/data/files`, а путь — в БД. Blazor Server по умолчанию ограничивает `IBrowserFile.OpenReadStream` 500 КБ.

## Related ADRs
- [[ApplicationDbContextAbstraction]] — тот же приём: интерфейс в Application, реализация в Infrastructure.

## Decisions

### 1. Файлы на диске, относительный путь в БД
**Decision.** `Karajob.Application.Files.IFileStorage` (`SaveAsync` / `OpenRead` / `Delete`) работает с относительными путями через `/`; `CvVersion.StoragePath` хранит именно его (`cv/{userId}/{guid}.pdf`). `LocalFileStorage` в Infrastructure резолвит путь от `Files:RootDirectory` (`/data/files` в контейнере, `data/files` от content root в Development) и отвергает пути вне корня. Имя файла на диске не зависит от имени, присланного клиентом; исходное имя лежит в `CvVersion.FileName`.

**Alternatives.**
- BLOB в SQLite — раздувает базу и её бэкап при каждой миграции, а переход на PostgreSQL усложняется.
- Абсолютный путь в БД — ломается при смене точки монтирования.

### 2. Порядок записи: файл → БД, удаления: БД → файл
**Decision.** При загрузке сначала пишется файл, потом `SaveChanges`; если БД упала, файл удаляется. При удалении сначала коммитится БД, потом удаляются файлы. В худшем случае остаётся лишний файл, но никогда — запись, указывающая на пропавший файл.

**Alternatives.**
- Транзакция поверх файловой системы — нет переносимого механизма, а выгода мала для однопользовательской установки.

### 3. Скачивание — Minimal API, не JS interop
**Decision.** `GET /cvs/versions/{id}/file` вызывает `CvService.OpenVersionAsync` и возвращает `Results.File` с `fileDownloadName`. В UI — обычная ссылка с атрибутом `download`: Blazor-роутер не перехватывает такие ссылки, и файл идёт обычным HTTP-ответом, а не через SignalR-circuit. Фильтр `UserId` действует и здесь: чужая версия — 404.

**Alternatives.**
- Стрим через `IJSRuntime` (`DotNetStreamReference`) — лишний JS и вся нагрузка на websocket.

### 4. Лимит размера из конфига, валидация в сервисе
**Decision.** `Cv:MaxFileSizeMb` (5) биндится в `CvOptions`. Web передаёт его в `OpenReadStream(maxAllowedSize)`, а `CvService` проверяет размер и расширение (`.pdf` / `.docx`) до открытия потока: `CvUpload.OpenRead` — делегат. Content-Type выводится из расширения, браузерному не доверяем.
