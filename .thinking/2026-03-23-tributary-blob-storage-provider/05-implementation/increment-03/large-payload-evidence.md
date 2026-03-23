# Large Payload Evidence

## Scope

- Increment: 3
- Evidence source:
  - `BlobEnvelopeCodecTests.LargePayloadMatrixShouldRoundTripDeterministicPayloadSizes(...)`
  - `dotnet test .\tests\Tributary.Runtime.Storage.Blob.L0Tests\Tributary.Runtime.Storage.Blob.L0Tests.csproj -c Release -warnaserror --filter "FullyQualifiedName~LargePayloadMatrixShouldRoundTripDeterministicPayloadSizes" --logger "console;verbosity=detailed"`
- Compression mode used for this matrix: `gzip`
- Payload generator: deterministic repeating byte pattern `index % 251`

## Buffering Model

- Encode path:
  - one uncompressed payload buffer from `SnapshotEnvelope.Data`
  - one optional compressed payload buffer when `gzip` is enabled
  - one final frame buffer containing prelude + header + stored payload bytes
- Decode path:
  - one stored-frame input buffer
  - one restored payload buffer after decompression
  - no secondary deserialization buffer inside the codec; the payload bytes are returned directly into the reconstructed `SnapshotEnvelope`
- Assessment:
  - The implementation stays within the planned bounded whole-payload buffering model for the current in-memory codec path.
  - Increment 4 still needs repository-path proof that exact-read/latest-read behavior does not introduce additional accidental payload copies.

## Matrix

| Payload size | Write | Read | Compression | Uploaded bytes | Downloaded bytes | Buffering model |
| --- | --- | --- | --- | ---: | ---: | --- |
| `256 KB` | Success | Success | `gzip` | `2047` | `2047` | payload buffer + optional compressed buffer + final frame buffer |
| `1 MB` | Success | Success | `gzip` | `5096` | `5096` | payload buffer + optional compressed buffer + final frame buffer |
| `5 MB` | Success | Success | `gzip` | `21367` | `21367` | payload buffer + optional compressed buffer + final frame buffer |
| `16 MB` | Success | Success | `gzip` | `66100` | `66100` | payload buffer + optional compressed buffer + final frame buffer |

## Observations

- All four deterministic payload sizes round-tripped successfully.
- The uploaded and downloaded byte counts are the measured stored-frame sizes emitted by the codec test output.
- The deterministic repeating payload pattern is intentionally highly compressible, so these byte counts demonstrate correctness of the frame and payload path rather than worst-case compression ratios.
- The stored header remained small enough to stay within the configured `MaxHeaderBytes` limit for every matrix entry.

## Raw Test Output Extracts

```text
payload=262144; compression=gzip; uploaded=2047; downloaded=2047; restored=262144; buffering=payload-buffer + optional-compressed-buffer + final-frame-buffer
payload=1048576; compression=gzip; uploaded=5096; downloaded=5096; restored=1048576; buffering=payload-buffer + optional-compressed-buffer + final-frame-buffer
payload=5242880; compression=gzip; uploaded=21367; downloaded=21367; restored=5242880; buffering=payload-buffer + optional-compressed-buffer + final-frame-buffer
payload=16777216; compression=gzip; uploaded=66100; downloaded=66100; restored=16777216; buffering=payload-buffer + optional-compressed-buffer + final-frame-buffer
```