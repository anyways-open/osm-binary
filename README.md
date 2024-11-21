# OsmSharp.IO.Binary

An IO module on top of OsmSharp that reads/writes OSM data in a **custom binary format**.

[![Build status](https://build.anyways.eu/app/rest/builds/buildType:(id:Osmsharp_OsmBinary)/statusIcon)](https://build.anyways.eu/viewType.html?buildTypeId=Osmsharp_OsmBinary)  

We built this because it more efficient compared to OSM-XML and has some advantages over OSM-PBF. This can be used to:

- Read/write individual objects.
- Read/write objects with negative ids
- Read/write objects missing data (even missing ids, versions, etc.).
- Can be streamed.
- Can be partially read when positions are known.
