using System;
using System.Runtime.Intrinsics.X86;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ecl;
/// <summary>
/// CRC-32 algorithm
/// </summary>
/// <cite>https://en.wikipedia.org/wiki/Cyclic_redundancy_check</cite>
public abstract class Crc32Calculator {
    public readonly uint Polynomial;

    private static TableCalculator _zipNormal;
    /// <summary>
    /// Normal CRC-32, for big endian
    /// </summary>
    public static Crc32Calculator ZipNormal => _zipNormal ??= new TableCalculator( 0x04C11DB7 );

    private static TableCalculator _zip;
    /// <summary>
    /// CRC-32 Reversed, used for little endian
    /// </summary>
    public static Crc32Calculator Zip => _zip ??= new TableCalculator( 0xEDB88320 );

    private static Crc32Calculator _castagnoli;
    /// <summary>
    /// CRC-32C Reversed, used for little endian
    /// </summary>
    public static Crc32Calculator Castagnoli => _castagnoli ??= new TableCalculator( 0x82F63B78 );


    private static Crc32Calculator _crc32c;
    /// <summary>
    /// CRC-32C Reversed, used for little endian
    /// </summary>
    public static Crc32Calculator Crc32c {
        get {
            if ( _crc32c == null ) {
                if ( Sse42.IsSupported ) {
                    _crc32c = new Sse42Crc32C();
                } else {
                    _crc32c = Castagnoli;
                }
            }
            return _crc32c;
        }
    }

    private static Crc32Calculator _castagnoliNormal;
    /// <summary>
    /// Normal CRC-32C, used for big endian
    /// </summary>
    public static Crc32Calculator CastagnoliNormal => _castagnoliNormal ??= new TableCalculator( 0x1EDC6F41 );

    private Crc32Calculator( uint polynomial ) {
        Polynomial = polynomial;
    }
    class Sse42Crc32C : Crc32Calculator {
        public Sse42Crc32C() : base( 0x82F63B78 ) {
        }
    }

    /// <summary>
    /// Compute CRC-32 using a table lookup
    /// </summary>
    class TableCalculator : Crc32Calculator {
        private readonly uint[] _table;

        public TableCalculator( uint polynomial ) : base( polynomial ) {
            var table = _table = new uint[ 16 * 256 ];
            for ( uint i = 0; i < 256; i++ ) {
                uint res = i;
                for ( int t = 0; t < 16; t++ ) {
                    for ( int k = 0; k < 8; k++ ) {
                        res = ( res & 1 ) != 0 ? polynomial ^ ( res >> 1 ) : ( res >> 1 );
                    }
                    table[ ( t * 256 ) + i ] = res;
                }
            }
        }

        private static uint Compute( ref uint table, uint crc, ref byte input, int length ) {
            uint crcLocal = ~crc;

            while ( length >= 16 ) {
                var a = Unsafe.Add( ref table, ( 3 * 256 ) + Unsafe.Add( ref input, 12 ) )
                    ^ Unsafe.Add( ref table, ( 2 * 256 ) + Unsafe.Add( ref input, 13 ) )
                    ^ Unsafe.Add( ref table, ( 1 * 256 ) + Unsafe.Add( ref input, 14 ) )
                    ^ Unsafe.Add( ref table, ( 0 * 256 ) + Unsafe.Add( ref input, 15 ) );

                var b = Unsafe.Add( ref table, ( 7 * 256 ) + Unsafe.Add( ref input, 8 ) )
                    ^ Unsafe.Add( ref table, ( 6 * 256 ) + Unsafe.Add( ref input, 9 ) )
                    ^ Unsafe.Add( ref table, ( 5 * 256 ) + Unsafe.Add( ref input, 10 ) )
                    ^ Unsafe.Add( ref table, ( 4 * 256 ) + Unsafe.Add( ref input, 11 ) );

                var c = Unsafe.Add( ref table, ( 11 * 256 ) + Unsafe.Add( ref input, 4 ) )
                    ^ Unsafe.Add( ref table, ( 10 * 256 ) + Unsafe.Add( ref input, 5 ) )
                    ^ Unsafe.Add( ref table, ( 9 * 256 ) + Unsafe.Add( ref input, 6 ) )
                    ^ Unsafe.Add( ref table, ( 8 * 256 ) + Unsafe.Add( ref input, 7 ) );

                var d = Unsafe.Add( ref table, ( 15 * 256 ) + ( (byte)crcLocal ^ input ) )
                    ^ Unsafe.Add( ref table, ( 14 * 256 ) + ( (byte)( crcLocal >> 8 ) ^ Unsafe.Add( ref input, 1 ) ) )
                    ^ Unsafe.Add( ref table, ( 13 * 256 ) + ( (byte)( crcLocal >> 16 ) ^ Unsafe.Add( ref input, 2 ) ) )
                    ^ Unsafe.Add( ref table, ( 12 * 256 ) + ( ( crcLocal >> 24 ) ^ Unsafe.Add( ref input, 3 ) ) );

                crcLocal = d ^ c ^ b ^ a;
                input = ref Unsafe.Add( ref input, 16 );
                length -= 16;
            }

            while ( --length >= 0 ) {
                crcLocal = Unsafe.Add( ref table, (byte)( crcLocal ^ input ) ) ^ crcLocal >> 8;
                input = ref Unsafe.Add( ref input, 1 );
            }

            return ~crcLocal;
        }
        public override uint Hash( ReadOnlySpan<byte> data, uint seed ) {
            return Compute( ref MemoryMarshal.GetArrayDataReference( _table ), seed,
                ref MemoryMarshal.GetReference( data ), data.Length );
        }
    }

    public static Crc32Calculator Get( uint polynomial ) {
        switch ( polynomial ) {
        case 0x04C11DB7:
            return ZipNormal;
        case 0xEDB88320:
            return Zip;
        case 0x82F63B78:
            return Crc32c;
        case 0x1EDC6F41:
            return CastagnoliNormal;
        }
        return new TableCalculator( polynomial );
    }
    public virtual uint Hash( ReadOnlySpan<byte> data, uint seed ) {
        int length = data.Length;
        seed = ~seed;
        ref byte src = ref MemoryMarshal.GetReference( data );
        if ( Sse42.X64.IsSupported ) {
            while ( length >= 8 ) {
                seed = (uint)Sse42.X64.Crc32( seed, Unsafe.ReadUnaligned<ulong>( ref src ) );
                src = ref Unsafe.Add( ref src, 8 );
                length -= 8;
            }
        }
        while ( length >= 4 ) {
            seed = Sse42.Crc32( seed, Unsafe.ReadUnaligned<uint>( ref src ) );
            src = ref Unsafe.Add( ref src, 4 );
            length -= 4;
        }
        while ( length > 0 ) {
            seed = Sse42.Crc32( seed, src );
            src = ref Unsafe.Add( ref src, 1 );
            length--;
        }
        return ~seed;
    }


}
