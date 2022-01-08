# CRC32
CRC-32 .net core calculator using Sse42 for CRC-32C if it is available

Benchmarks

| Method |      Mean |     Error |    StdDev |
|------- |----------:|----------:|----------:|
|  Table | 24.985 us | 0.4861 us | 0.4992 us |
| Vector |  9.142 us | 0.1219 us | 0.1140 us |
|   Fast | 45.424 us | 0.9008 us | 1.5538 us |

##### Where:
**Table** is using the conventional computation using a lookup table

**Vector** is the accelerated computation using Sse42 intrinsics. Note that only CRC-32C is supported ( polynomial = 0x82F63B78 )

**Fast** is the fastest implementation I could find for .NET Core. Implementation is here [force-net/Crc32.NET](https://github.com/force-net/Crc32.NET)
