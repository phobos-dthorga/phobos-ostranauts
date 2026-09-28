# Claude handoff: direct PixelLab MCP access

Prepared 29 September 2026 for phobosgekko. Give this document to Claude Code
in this repository, alongside the [project handoff](claude-code-handoff.md).
The task is to connect Claude to PixelLab's actual MCP server and verify access.
An artwork brief, downloaded images or a list of tool names does not provide
that connection. This handoff does not request new artwork.

## Connection contract

PixelLab's [official setup instructions](https://www.pixellab.ai/mcp) specify:

- Endpoint: `https://api.pixellab.ai/mcp`
- Transport: HTTP (Streamable HTTP).
- Authentication: `Authorization: Bearer <PixelLab secret>`.
- Obtain the secret by signing in through PixelLab's setup page.
- [PixelLab's live tool documentation](https://api.pixellab.ai/mcp/docs)
  describes the available operations; discover their current schemas in Claude.

Connect directly to this official service. No third-party wrapper, local Python
server, browser automation or Codex proxy is needed. Codex's connected tools are
not automatically available inside a separate Claude session. Do not extract
credentials from Codex settings, logs or another project's configuration.

## Verified here; still to verify in Claude

The originating Codex session successfully called PixelLab `get_balance` on
29 September 2026. It reported an active subscription. No generation was
submitted, no credits purchased and no account settings changed.

Claude access has **not** been verified. The `claude` command was not found on
the originating shell's PATH; that does not establish whether Claude is available
in another terminal, editor, WSL environment or desktop client. Use the receiving
Claude environment. These commands target **Claude Code**, not Claude chat's
connector settings. Do not claim setup succeeded merely because this file exists.

## Setup for the receiving Claude Code session

1. Read [AGENTS.md](../../AGENTS.md). Inspect existing MCP server names and status
   without printing secrets; preserve other connections. If PixelLab is already
   connected, proceed straight to verification.
2. Open a terminal at the repository root in the environment that launches
   Claude Code. Confirm `claude --version` works there.
3. If no PixelLab entry exists, register it with a literal environment reference:

   ```powershell
   claude mcp add --transport http --scope local --header 'Authorization: Bearer ${PIXELLAB_API_KEY}' pixellab https://api.pixellab.ai/mcp
   ```

   Keep the single quotes in PowerShell: the saved header must contain the
   placeholder, not an expanded secret. Check an existing entry before replacing
   it; do not create a duplicate that masks a working connection.
4. The **owner**, in their own PowerShell 7 terminal, enters the PixelLab secret
   and launches Claude from that same terminal:

   ```powershell
   $env:PIXELLAB_API_KEY = Read-Host 'PixelLab API secret' -MaskInput
   try {
       claude
   } finally {
       Remove-Item Env:PIXELLAB_API_KEY -ErrorAction SilentlyContinue
   }
   ```

   This is a manual owner step, not an instruction for an agent to collect or
   enter the credential. Do not paste the secret into chat or a tracked file.
   The variable is temporary; repeat entry for a later launch. An already-running
   editor does not inherit a newly set terminal variable. If using WSL, provision
   the variable privately inside that environment instead.
5. In Claude, open `/mcp`, then perform the verification below.

Anthropic's [MCP reference](https://code.claude.com/docs/en/mcp) documents local
scope, HTTP headers, environment substitution and `/mcp`. Local scope keeps the
entry private to this checkout. If the installed client cannot expand the header,
check its version and current documentation rather than replacing the placeholder
with a secret in shared files.

Equivalent configuration shape for diagnosis or a deliberately chosen
project-scoped `.mcp.json` (merge with existing servers; do not overwrite them):

```json
{
  "mcpServers": {
    "pixellab": {
      "type": "http",
      "url": "https://api.pixellab.ai/mcp",
      "headers": {
        "Authorization": "Bearer ${PIXELLAB_API_KEY}"
      }
    }
  }
}
```

## Acceptance check for Claude

- Confirm `/mcp` reports PixelLab connected and exposes actual callable tools.
- Invoke `get_balance` through that connection. Report success and whether the
  subscription is active; do not publish private account details into Git.
- Discover the relevant generation, retrieval and status tool schemas. Tool
  prefixes depend on the client; Codex's `mcp__pixellab__` prefix is not required.
- Stop the access check there. Do not create a sample asset just to test access.
- Report separately: configuration present, authenticated call successful, and
  generation not attempted. On failure, give the sanitized error and next step.

For authentication failure, the owner should check the secret and whether the
Claude process inherited its variable. For missing tools, inspect connection
status and reconnect/restart the receiving client. Do not dump headers, full
credential files or environment values into diagnostics. Do not bypass client
trust prompts or replace the official endpoint with an unrelated service.

## Continuing authorized artwork work

Read the actual artwork task and [asset generation policy](asset-generation-policy.md),
[resolution policy](artwork-resolution-policy.md), and owning asset manifests.
Reuse accepted masters. For world sprites, use the policy's overhead-first prompt
and supported `view="high top-down"`, `isometric=false` controls. Inspect one
pilot at native size before extending a family; reject visible vertical faces or
diamond projection. UI faceplates have a separate camera exception.

Check live allowance and operation cost before an authorized generation. Preserve
exact requests, seeds when provided, returned job/asset IDs, untouched masters,
hashes and review decisions. Follow the chosen tool's asynchronous retrieval
workflow; retain the returned ID instead of resubmitting an unfinished job.
Connection setup alone authorizes neither paid generation nor uploads/publication.

## Prompt to give Claude

> Read AGENTS.md, docs/development/claude-code-handoff.md and
> docs/development/claude-pixellab-mcp-handoff.md. Establish direct access to the
> official PixelLab MCP server in your own environment. Preserve existing MCP
> connections and keep credentials private; credential entry is my manual step.
> Verify access by calling get_balance and discovering the live tool schemas.
> Do not generate artwork as a connection test. Report what you actually verified
> and any remaining setup step, then continue only the artwork work I authorize.
