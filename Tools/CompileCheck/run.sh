#!/bin/bash
# usage: ./run.sh "DEFINES"
cd "$(dirname "$0")"
REFS=$(for f in /usr/lib/dotnet/packs/Microsoft.NETCore.App.Ref/8.0.32/ref/net8.0/*.dll; do printf -- "-r:%s " "$f"; done)
dotnet /usr/lib/dotnet/sdk/8.0.132/Roslyn/bincore/csc.dll -nologo -noconfig -target:library -out:/tmp/claude-0/check.dll -nowarn:CS0618,CS0067,CS0649,CS0414,CS0169 -langversion:9 -define:"$1" $REFS Stubs.cs $(find ../../Assets -name '*.cs')
