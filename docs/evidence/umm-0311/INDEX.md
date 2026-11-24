# UMM 0.31.1 baseline evidence

Machine-generated outputs for candidate commit `c0a6732396d1d000b771d3dcd9abed35b7d1525e`, exported 2026-11-24 01:06 UTC.
Only local path prefixes were replaced: `<repo>` (this checkout or its temporary clone root), `<temp>`, `<game>`
(the Kingmaker installation, read-only), `<buildtools>`, `<reference-assemblies>`, `<dotnet>`, `<controller>` (the agent-review controller), `<dev>` (the owner's development folder), `<steam>`, `<user-profile>`, `<fleet>` (the owner's fleet mission folder,
outside this repository). Line endings were normalized to LF and a UTF-8 byte-order mark was dropped; nothing else was
edited. Raw SHA-256 is of the original output bytes. Exported SHA-256 is of the committed Git blob of each file in this
folder (LF bytes; a Windows checkout with `core.autocrlf` may show CRLF in the working tree, so hash the blob, not the
checked-out file). To verify one, from the repository root:

```
git cat-file blob HEAD:docs/evidence/umm-0311/<file> | sha256sum
python -c "import hashlib,subprocess,sys; print(hashlib.sha256(subprocess.check_output(['git','cat-file','blob','HEAD:docs/evidence/umm-0311/'+sys.argv[1]])).hexdigest())" <file>
```

| File | Raw SHA-256 | Exported SHA-256 (Git blob) |
| --- | --- | --- |
| `build-receipt.json` | `8365e73a730850fefb6c0d14f7dc4b26e89dddbdcba5ad4aaced6a9d39c97156` | `7e59cb5a4ff345b9872bba08d60de5b06e83836e13f980d88c0ae286ced26364` |
| `package-validation.json` | `36b3c2878441642d475b942ad61cbc5b3b5ebc69864cd09942c3a7a6187a6c0d` | `c905f6f95ca68f802b6b00bca71ad4eac5a3e670aeabd0698893e1ebba51a387` |
| `direct-build-record.json` | `d6def52ed56ed51a020c8406eb7b2cf849819f435a5b8dbc3651363589c575bd` | `9de9bc95fab77cb9e7940222e11f4fa8d3b2d1d7f48f96b7713a785ef3d7a02d` |
| `loader-baseline.json` | `58cf87de1b740b153b0456f94fa6f7270f584ab4825781023111a7e8d74bc2a7` | `8340e5121326d640238fbcb1ab70a4178936df274591b29557e54ee2435c2646` |
| `api-audit.json` | `1c85d73828cdc990ac19aac041f0e28ff2040678f8242f0bc0f85346f9242283` | `aad38b1472d791d61be73fdc726db8c9c10c35e5135facaa36685de3fab80760` |
| `loader-profile-inventory.json` | `578b4b2a8605dd027af359444dd12ba8edda8d45e8cea6ed0d8b953636cac5aa` | `370ad2c8904906afc023e660769d4f77c66c0d30f73fdf16f2dba39c6161beeb` |
| `docs-link-check.json` | `33c6d5549b2382f54fdbb197ad2859315e9e1f44aa445d935749cd9a129855d7` | `4ea04cf7c5b89e9c7088afc6092e9affeda2817a97341fec4ed9f7374a3ea125` |
| `onboarding-check-project.json` | `62de07c8b77eb26a1d127ac0e2b864237747fb8580eb8afedbe0ccb4277b119f` | `41173a29b581cb70452e4c1ddee0f684b3d0bc022eccb72ed297e5913de3edb3` |
| `onboarding-preflight.json` | `866b925516364db6a5ca660090f7250b8c34a337b1c9d5e1a98fc8cb3d4a7d25` | `48b7b83c7e2321bd53e392898e5e8cb40f360b943d04247929fbb45824c4b9e4` |
| `onboarding-profile-summary.json` | `bd865902fc3b4229cd2a7fb66530d0a485cd8837e96001b761cb586b42e018e0` | `805c79fe888e0c79367200bc5982cef230023a81228a1e15590a53639424c345` |
| `pr-12-description.json` | `894f726391f0075f5439a59ff2c64b84218f6b4db980e6d6a874700fd2d3074b` | `8b6599209c25759b375fe15a728b08f769003009deeebe77aa1385698a5d50e8` |
| `newest-stable-selection.json` | `3b3afcb904f00b33006d74c481fbce4bf6e4992e7f9001d28cdf28d589f9f56f` | `150806fc23701c046ae9980c73c91d12c3badb671d7e30c126b61060784dd51e` |
| `loader-profiles.md` | `3b2883677f537286a0d6f210327fae8ac8734b40ec7cc5850e92819eaeb03d5e` | `3b2883677f537286a0d6f210327fae8ac8734b40ec7cc5850e92819eaeb03d5e` |
| `loader-api-audit-Program.cs.txt` | `be870827024fd41f63235ebb522c489e8d49c7991de7091ad0e14a1a0e935ea3` | `be870827024fd41f63235ebb522c489e8d49c7991de7091ad0e14a1a0e935ea3` |
| `loader-api-audit-csproj.txt` | `93989652d8252ff54a60ec92c13c1fc41bf94bfa3cf6c9f7f13c877bac761909` | `93989652d8252ff54a60ec92c13c1fc41bf94bfa3cf6c9f7f13c877bac761909` |
