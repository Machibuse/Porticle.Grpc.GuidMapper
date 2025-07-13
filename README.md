# Porticle.Grpc.GuidMapper

A Roslyn-based post-processor for protoc-generated files that adds native Guid, Guid? and string? types in gRPC services.

## Overview

This library allows you to automatically convert PROTO string or StringValue fields to C# Guid, Guid? or string? types in protoc-generated files, enabling seamless integration of
GUIDs in your gRPC services without manual conversion
code.

## Installation

### Install the package via NuGet:
```powershell
dotnet add package Porticle.Grpc.GuidMapper
```

After installing the Package, this Post build step ist dynamically added to your build.

```msbuild
<Project>
    <Target Name="RunProtoPostProcessing" AfterTargets="Protobuf_Compile">
        <ItemGroup>
            <_FilesToPostProcess Include="$(MSBuildProjectDirectory)\%(Protobuf_Compile.OutputDir)\%(Protobuf_Compile.Filename)" />
        </ItemGroup>
        <Message Text="Proto Postprocessing" Importance="high" />
        <Exec Command="dotnet &quot;$(MSBuildThisFileDirectory)..\tools\$(TargetFramework)\Porticle.Grpc.GuidMapper.dll&quot; -- %(_FilesToPostProcess.Identity)" Condition="'@(_FilesToPostProcess)' != ''" />
    </Target>
</Project>
```

Dont wonder ist you cant se it in your csproj file. It is dynamically added when your build is processed.  

## Usage

There are three things you can do in your .proto files:
- Add `// [GrpcGuid]` as comment to a string field - Converts the corresponding c# string property to Guid
- Add `// [GrpcGuid]` as comment to a StringValue field - Converts the corresponding c# string property to Guid?
- Add `// [NullableString]` as comment to a StringValue field - Converts the corresponding c# string property to string?


First an Example of a default .proto file

### Without GuidMapper
```protobuf
syntax = "proto3";

import "google/protobuf/wrappers.proto";

message User {
  // Guid of the user object   
  string id = 1;

  // Optional reference ID
  google.protobuf.StringValue optional_reference_id = 4;

  // Name
  string name = 2;

  // Optional description
  google.protobuf.StringValue description = 3;
}
```
Will result in generated code like this
```csharp
/// <summary>Guid of the user object</summary>
public string Id {
  get { return id_; }
  set { id_ = pb::ProtoPreconditions.CheckNotNull(value, "value"); }
}

/// <summary>Optional reference ID</summary>
public string OptionalReferenceId {
  get { return optionalReferenceId_; }
  set { optionalReferenceId_ = value; }
}

/// <summary>Name</summary>
public string Name {
  get { return name_; }
  set { name_ = pb::ProtoPreconditions.CheckNotNull(value, "value"); }
}

/// <summary>Optional description</summary>
public string Description {
  get { return description_; }
  set { description_ = value; }
}
```

### With GuidMapper
```protobuf
syntax = "proto3";

import "google/protobuf/wrappers.proto";

message User {
  // [GrpcGuid] Guid of the user object   
  string id = 1;

  // [GrpcGuid] Optional reference ID
  google.protobuf.StringValue optional_reference_id = 4;

  // Name
  string name = 2;

  // [NullableString] Optional description
  google.protobuf.StringValue description = 3;
}
```
Will result in generated code like this
```csharp
/// <summary>[GrpcGuid] Guid of the user object</summary>
public global::System.Guid Id {
  get {return global::System.Guid.Parse(id_); }
  set { id_ = (value).ToString("D"); }
}

/// <summary>[GrpcGuid] Optional reference ID</summary>
public global::System.Guid? OptionalReferenceId {
  get {if(optionalReferenceId_==null)return default; return global::System.Guid.Parse(optionalReferenceId_); }
  set { optionalReferenceId_ = (value)?.ToString("D"); }
}

/// <summary>Name</summary>
public string Name {
  get { return name_; }
  set { name_ = pb::ProtoPreconditions.CheckNotNull(value, "value"); }
}

#nullable enable
/// <summary>[NullableString] Optional description</summary>
public string? Description {
  get { return description_; }
  set { description_ = value; }
}
#nullable disable
```
