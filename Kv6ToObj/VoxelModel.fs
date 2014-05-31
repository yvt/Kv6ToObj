
module Kv6ToObj.VoxelModel

open System
open System.IO

type Model(w, h, d) =
    let data : uint32 array = Array.zeroCreate(w * h * d)

    member this.size : int*int*int = (w, h, d)
    member this.Item
        with get (x, y, z) = 
            if x >= 0 && y >= 0 && z >= 0 && x < w && y < h && z < d then
                data.[((x * h) + y) * d + z]
            else uint32 0
        and set (x, y, z) value = data.[((x * h) + y) * d + z] <- value


type Kv6Block = { color : uint32; zPos : uint16; visFaces : byte; lighting : byte }

let loadFromKv6 (stream: Stream) =
    let reader = new BinaryReader(stream)
    let asc = new System.Text.ASCIIEncoding()
    let hdrbuf : byte array = Array.zeroCreate 4
    if stream.Read(hdrbuf, 0, 4) < 4 then
        failwith "Magic not read"
    else ()
    if asc.GetString(hdrbuf) <> "Kvxl" then 
        failwith "Invalid magic"
    else ()

    let xsiz = reader.ReadInt32()
    let ysiz = reader.ReadInt32()
    let zsiz = reader.ReadInt32()
    let xpivot = reader.ReadSingle()
    let ypivot = reader.ReadSingle()
    let zpivot = reader.ReadSingle()
    let numBlocks = reader.ReadInt32()

    let blkdata : Kv6Block array = 
        Array.init numBlocks (fun(i) ->
            {
                color = reader.ReadUInt32()
                zPos = reader.ReadUInt16()
                visFaces = reader.ReadByte()
                lighting = reader.ReadByte()
            }
        )

    let xoffset = 
        Array.init xsiz (fun (i) ->
            reader.ReadUInt32()
        )
    let xyoffset = 
        Array.init (xsiz * ysiz) (fun (i) ->
            reader.ReadUInt16()
        )

    let mutable pos = 0
    let model = Model(xsiz, ysiz, zsiz)
    for x = 0 to (xsiz - 1) do
        for y = 0 to (ysiz - 1) do
            let spanBlocks = xyoffset.[x * ysiz + y]
            for i = 1 to int spanBlocks do
                let b = blkdata.[pos]
                model.[x, y, int b.zPos] <- b.color ||| uint32 0xff000000
                pos <- pos + 1

    
    (model, Geometry.vec3(float xpivot, float ypivot, float zpivot))

