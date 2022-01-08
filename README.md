# CRC32
CRC-32 for .net core. Is using Sse42 for CRC-32C if it is available

Benchmarks

| Method |      Mean |     Error |    StdDev |
|------- |----------:|----------:|----------:|
| Table  | 24.985 us | 0.4861 us | 0.4992 us |
| Sse42  |  9.142 us | 0.1219 us | 0.1140 us |
| Force  | 45.424 us | 0.9008 us | 1.5538 us |

##### Where:
**Table** is using the conventional computation with a lookup table

**Sse42** is the accelerated computation using Sse42 intrinsics. Note that only CRC-32C is supported ( polynomial = 0x82F63B78 )

**Force** is the fastest implementation I could find for .NET Core. The repository is here [force-net/Crc32.NET](https://github.com/force-net/Crc32.NET), also some code for table computation derives from there.
