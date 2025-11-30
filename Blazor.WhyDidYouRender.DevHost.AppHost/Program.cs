var builder = DistributedApplication.CreateBuilder(args);

// builder.AddProject<Projects.Blazor_WhyDidYouRender_DevHost_AppHost>("webfrontend")
// 	.WithExternalHttpEndpoints();

builder.AddProject<Projects.RenderTracker_WasmSampleApp>("wasm-sample");
builder.AddProject<Projects.RenderTracker_SampleApp>("server-sample");

builder.Build().Run();
