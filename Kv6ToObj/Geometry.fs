
module Kv6ToObj.Geometry

open System;

type Vector3 = 
    { x: float; y: float; z: float }
    static member (+) (a:Vector3, b:Vector3) = {x = a.x+b.x; y=a.y+b.y; z=a.z+b.z}
    static member (-) (a:Vector3, b:Vector3) = {x = a.x-b.x; y=a.y-b.y; z=a.z-b.z}
    static member (*) (a:Vector3, b:Vector3) = {x = a.x*b.x; y=a.y*b.y; z=a.z*b.z}
    static member (/) (a:Vector3, b:Vector3) = {x = a.x/b.x; y=a.y/b.y; z=a.z/b.z}

let inline vec3 (x, y, z):Vector3 = {x=x; y=y; z=z}

