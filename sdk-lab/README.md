# Huefy .NET SDK Lab

Verifies the core email contract through the real `.NET` email client against a local stub server.

## Run

```bash
dotnet run
```

from `sdks/dotnet/sdk-lab/`.

## Scenarios

1. Initialization
2. Single-send contract shaping
3. Bulk-send contract shaping
4. Validation rejection for invalid single input
5. Validation rejection for invalid bulk input
6. SDK health path behavior
7. Cleanup

## Notes

- The lab goes through the real `HuefyEmailClient`.
- It checks serialized request bodies, parsed responses, and validation-before-transport behavior.
