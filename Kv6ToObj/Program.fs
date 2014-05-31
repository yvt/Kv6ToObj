
// NOTE: If warnings appear, you may need to retarget this project to .NET 4.0. Show the Solution
// Pad, right-click on the project node, choose 'Options --> Build --> General' and change the target
// framework to .NET 4.0 or .NET 4.5.

module Kv6ToObj.Main

open System
open Geometry
open System.Collections.Generic

type Face = 
    { vertices : Vector3*Vector3*Vector3*Vector3; color : uint32 }

let saveObj (faces: Face array) outFile defColor =
    let textureSize = Math.Sqrt(float faces.Length) |> Math.Ceiling |> int
    let pth = System.IO.Path.GetDirectoryName(outFile)
    let mtlName = System.IO.Path.GetFileNameWithoutExtension(outFile) + ".mtl"
    let imgName = System.IO.Path.GetFileNameWithoutExtension(outFile) + ".png"
    use writer = System.IO.File.CreateText(outFile)
    writer.WriteLine("mtllib " + mtlName)
    writer.WriteLine("o Object")

    let img = new System.Drawing.Bitmap(textureSize * 4, textureSize * 4, System.Drawing.Imaging.PixelFormat.Format32bppRgb)

    for face in faces do
        let inline emit (v:Vector3) =
            writer.WriteLine("v {0} {1} {2}", v.x, -v.z, v.y)
        let v1,v2,v3,v4 = face.vertices
        emit v1; emit v2; emit v3; emit v4
    for i = 0 to faces.Length - 1 do
        let face = faces.[i]
        let yy = i / textureSize
        let xx = i - yy * textureSize
        let col = System.Drawing.Color.FromArgb(int face.color)

        let col = 
            if col.R = byte 0 && col.G = byte 0 && col.B = byte 0 then
                defColor
            else col

        for x = 0 to 3 do 
            for y = 0 to 3 do
                img.SetPixel(x + xx*4, y+yy*4, col)

        let xx = (float xx + 0.5) / (float textureSize)
        let yy = 1.0 - (float yy + 0.5) / (float textureSize)
        writer.WriteLine("vt {0} {1}", xx, yy)
    
    writer.WriteLine("usemtl Material")
    writer.WriteLine("s off")
   
    for i = 0 to faces.Length-1 do
        writer.WriteLine("f {0}/{1} {2}/{3} {4}/{5} {6}/{7}",
                         1+i*4, i+1, 2+i*4, i+1, 3+i*4, i+1, 4+i*4, i+1) (*
        writer.WriteLine("f {0}/{1} {2}/{3} {4}/{5}",
                         1+i*4, i+1, 2+i*4, i+1, 3+i*4, i+1)
        writer.WriteLine("f {0}/{1} {2}/{3} {4}/{5}",
                         3+i*4, i+1, 4+i*4, i+1, 1+i*4, i+1)*)

    
    use mwriter = System.IO.File.CreateText(System.IO.Path.Combine(pth, mtlName))
    mwriter.WriteLine("newmtl Material")
    mwriter.WriteLine("Ka 0 0 0")
    mwriter.WriteLine("Kd 1 1 1")
    mwriter.WriteLine("Ks 0.2 0.2 0.2")
    mwriter.WriteLine("Ni 1")
    mwriter.WriteLine("d 1")
    mwriter.WriteLine("illum 2")
    mwriter.WriteLine("map_Kd {0}", imgName)


    img.Save(System.IO.Path.Combine(pth, imgName), System.Drawing.Imaging.ImageFormat.Png)
    ()



let runProcess inFile outFile defColor =
    let model, pivot =
        using (System.IO.File.OpenRead(inFile)) (fun f ->
            VoxelModel.loadFromKv6 f
        )
    let faces = List<Face>()
    let (w:int, h:int, d:int) = model.size

    for x = 0 to (w - 1) do
        for y = 0 to (h - 1) do
            for z = 0 to (d - 1) do
                let col = model.[x, y, z]
                let inline emit (x1,y1,z1,x2,y2,z2,x3,y3,z3,x4,y4,z4) =
                    {
                        vertices = (
                                    vec3(float x1, float y1, float z1) - pivot,
                                    vec3(float x2, float y2, float z2) - pivot,
                                    vec3(float x3, float y3, float z3) - pivot,
                                    vec3(float x4, float y4, float z4) - pivot
                                   )
                        color = col
                    } |> faces.Add
                let rec emit2 (x, y, z, ux, uy, uz, vx, vy, vz, flip) =
                    if not flip then
                        emit2(x + ux, y + uy, z + uz,
                          -ux, -uy, -uz, vx, vy, vz, true)
                    else
                        emit(
                                x, y, z, 
                                x + ux, y + uy, z + uz,
                                x + ux + vx, y + uy + vy, z + uz + vz,
                                x + vx, y + vy, z + vz
                            )
                if col <> uint32 0 then
                    if model.[x - 1, y, z] = uint32 0 then
                        emit2(x, y, z, 0, 1, 0, 0, 0, 1, false)
                    if model.[x + 1, y, z] = uint32 0 then
                        emit2(x+1, y, z, 0, 1, 0, 0, 0, 1, true)
                    if model.[x, y - 1, z] = uint32 0 then
                        emit2(x, y, z, 1, 0, 0, 0, 0, 1, true)
                    if model.[x, y + 1, z] = uint32 0 then
                        emit2(x, y + 1, z, 1, 0, 0, 0, 0, 1, false)
                    if model.[x, y, z - 1] = uint32 0 then
                        emit2(x, y, z, 1, 0, 0, 0, 1, 0, false)
                    if model.[x, y, z + 1] = uint32 0 then
                        emit2(x, y, z + 1, 1, 0, 0, 0, 1, 0, true)
                ()
    saveObj (faces.ToArray()) outFile defColor

[<EntryPoint>]
let main args = 
    if args.Length < 5 then
        Console.WriteLine("Argument missing")
    else
        let parse = Int32.Parse
        runProcess args.[0] args.[1] (System.Drawing.Color.FromArgb(parse(args.[2]), parse(args.[3]), parse(args.[4])))
    0

