# iPhone Shortcuts: quick add to Inbox

A Shortcuts command sends a vacancy link to Karajob and it lands in your Inbox. Duplicates are detected by URL.

## 1. Get a token

1. Open Karajob in a browser → profile menu → **Profile settings** → **API token**.
2. Press **Generate**, then copy the token (it starts with `kj_`). It is shown **once**; only its hash is stored. If you lose it, press **Regenerate**.
3. **Regenerate** makes the old token stop working immediately; **Revoke** removes the token, and the API is off for your profile until you generate a new one.

Each profile has its own token and the vacancy goes to the Inbox of the profile that owns the token. The token is an address, not strong protection: keep the app on the home network (or behind WireGuard).

## 2. Create the shortcut

In the Shortcuts app create a new shortcut, enable **Show in Share Sheet** (accept URLs), and add the actions:

1. **Get Contents of URL**
   - URL: `http://<karajob-host>:<port>/api/inbox`, the address the app is actually served on (the same one you open in the browser)
   - Method: `POST`
   - Headers: `Authorization` = `Bearer kj_…` (your token)
   - Request Body: `JSON`
     - `url` (Text) = *Shortcut Input*
     - `note` (Text) = optional; add an **Ask for Input** action before it if you want a note
2. **Get Dictionary Value** → `message` from *Contents of URL*.
3. **Show Notification** (or **Show Result**) with that value.

## Responses

| Status | Meaning | Body |
| --- | --- | --- |
| `201` | A new vacancy was added to Inbox | `{ "vacancyId", "alreadyExisted": false, "stage", "message" }` |
| `200` | The URL is already in the tracker | same shape, `"alreadyExisted": true` |
| `400` | The URL is invalid (only absolute http(s) links) | `{ "errors": { … } }` |
| `401` | Missing, wrong or revoked token | empty |

## Check from a terminal

```powershell
curl.exe -X POST http://<karajob-host>:<port>/api/inbox -H "Authorization: Bearer kj_..." -H "Content-Type: application/json" -d '{\"url\":\"https://example.com/jobs/1\",\"note\":\"from phone\"}'
```

HTTPS and the "Share → Karajob" PWA flow for Android come with #49.
