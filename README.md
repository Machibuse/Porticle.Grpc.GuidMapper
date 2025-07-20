# Porticle.Grpc.GuidMapper

[![Build and Release](https://github.com/Machibuse/Porticle.Grpc.GuidMapper/actions/workflows/release.yaml/badge.svg)](https://github.com/Machibuse/Porticle.CLDR/actions/workflows/release.yaml)  

State on nuget.org [![NuGet Latest Version](https://img.shields.io/nuget/v/Porticle.Grpc.GuidMapper.svg)](https://www.nuget.org/packages/Porticle.Grpc.GuidMapper/)  [![NuGet Downloads](https://img.shields.io/nuget/dt/Porticle.Grpc.GuidMapper.svg)](https://www.nuget.org/packages/Porticle.CLDR.Units/)

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

  // Optional parent UserId
  google.protobuf.StringValue optional_parent_user_id = 2;

  // Optional description
  google.protobuf.StringValue description = 3;

  // List of roles 
  repeated string role_ids = 4;
}
```
Will result in generated code like this, everything is a string
```csharp
/// <summary>Guid of the user object</summary>
public string Id {
  get { return id_; }
  set { id_ = pb::ProtoPreconditions.CheckNotNull(value, "value"); }
}

/// <summary>Optional Guid of the parent UserId</summary>
public string OptionalParentUserId {
  get { return optionalParentUserId_; }
  set { optionalParentUserId_ = value; }
}

/// <summary>Optional description string</summary>
public string Description {
  get { return description_; }
  set { description_ = value; }
}

/// <summary>List of roles</summary>
public pbc::RepeatedField<string> RoleIds {
  get { return roleIds_; }
}
```

### With GuidMapper
```protobuf
syntax = "proto3";

import "google/protobuf/wrappers.proto";

message User {
  // [GrpcGuid] Guid of the user object   
  string id = 1;

  // [GrpcGuid] Optional Guid of the parent UserId
  google.protobuf.StringValue optional_parent_user_id = 2;

  // [NullableString] Optional description string
  google.protobuf.StringValue description = 3;

  // [GrpcGuid] List of roles 
  repeated string role_ids = 4;
}
```
Will result in generated code like this, using string? Guid and Guid?
```csharp
/// <summary>[GrpcGuid] Guid of the user object</summary>
public global::System.Guid Id {
  get { return global::System.Guid.Parse(id_); }
  set { id_ = (value).ToString("D"); }
}

/// <summary>[GrpcGuid] Optional Guid of the parent UserId</summary>
public global::System.Guid? OptionalParentUserId {
  get { if(optionalParentUserId_==null) return default; return global::System.Guid.Parse(optionalParentUserId_); }
  set { optionalParentUserId_ = (value)?.ToString("D"); }
}


#nullable enable
/// <summary>[NullableString] Optional description string</summary>
public string? Description {
  get { return description_; }
  set { description_ = value; }
}
#nullable disable

/// <summary>[GrpcGuid] List of roles</summary>
public IList<Guid> RoleIds {
  get {return new RepeatedFieldGuidWrapper(roleIds_); }
}
```

## What currently is not Possible?

- Mapping `repeated google.protobuf.StringValue` to `List<Guid?>` or `List<string?>` because grpc internally uses `RepeatedField<string>` instead of `RepeatedField<StringValue>`.
  This may be a bug in protoc compiler, because it is also not possible to add `null` to `repeated google.protobuf.StringValue` because ther is a not null check in the Add function in `RepeatedField<T>`

- This Tool actually don't works when protoc / Grpc.Tools is compiled with GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE