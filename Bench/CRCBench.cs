using BenchmarkDotNet.Attributes;
using ecl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics.Arm;
using System.Text;
using System.Threading.Tasks;

namespace Bench;

public class CRCBench {
    byte[] _data;
    [GlobalSetup]
    public void GlobalSetup() {
        Random m = new Random();
        byte[] data = new byte[ 65536 - 7 ];
        m.NextBytes( data );
        _data = data;
    }

    [Benchmark]
    public void Table() {
        Crc32Calculator.Castagnoli.Hash( _data, 0 );
    }

    [Benchmark]
    public void Vector() {
        Crc32Calculator.Crc32c.Hash( _data, 0 );
}
    
    [Benchmark]
    public void Fast() {
        Force.Crc32.Crc32CAlgorithm.Compute( _data );
    }
}
