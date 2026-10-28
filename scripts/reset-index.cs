#!/usr/bin/env dotnet
#:project ../src/Standup.Core/Standup.Core.csproj
#:property PublishAot=false
#:property NoWarn=EXTEXP0019;MEAI001;MEAI002;MEAI003;MEDE0001

using Standup;

using var index = new EvidenceIndex(args.FirstOrDefault() ?? DemoConfiguration.Provider);
index.Invalidate();
await index.Collection.EnsureCollectionDeletedAsync();
Console.WriteLine($"Deleted only current demo collection: {index.Name}");
Console.WriteLine("No fixture, other provider index, private receipt or cloud resource was changed.");
