using BenchmarkDotNet.Running;
using ecl;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Bench;

internal class Program {
	static void Main( string[] args ) {
		//TestValid();
		BenchmarkRunner.Run( typeof( CRCBench ) );
	}

	private static void TestValid() {
		Random m = new Random();
		byte[] data = new byte[ 65534 ];
		m.NextBytes( data );

		uint crc32c = Crc32Calculator.Castagnoli.Hash( data, 0 ); // use the table lookup version
		uint sse42 = Sse42( data ); // use the Sse42
		uint force = Force.Crc32.Crc32CAlgorithm.Compute( data ); // use Crc32.NET
		Debug.WriteLine( $"Std:{crc32c:X8}, sse42:{sse42:X8}, Force:{force:X8}" );
	}
	public static uint Sse42( ReadOnlySpan<byte> data ) {
		return Crc32Calculator.Crc32c.Hash( data, 0 );
		//return TestCRC.Hash.Crc32.Hash( data, 0 );
	}
	
}
