# Cross-Platform Testing Guide

## 🎯 Testing Objectives

Verify that WhyDidYouRender works correctly in both Server and WASM environments with identical functionality.

## 🖥️ Server-Side Testing

### 1. Start the Server Sample App
```bash
cd RenderTracker.SampleApp
dotnet run
```

### 2. Test Server-Side Features
1. **Navigate to**: `https://localhost:5001/cross-platform-demo`
2. **Check Server Console**: Should see WhyDidYouRender initialization messages
3. **Check Browser Console**: Should see browser console logging (if enabled)
4. **Test Interactions**:
   - Click "Server Increment" - should log render events
   - Click "Get Server Time" - should show real-time server updates
   - Click "Update Child Title" - should track parameter changes

### 3. Expected Server-Side Behavior
- ✅ Server console shows render tracking
- ✅ Browser console shows render events (if configured)
- ✅ Session management uses HttpContext
- ✅ Real-time server time updates
- ✅ Component inheritance works with TrackedComponentBase

## 🌐 WASM Testing

### 1. Start the WASM Sample App
```bash
cd RenderTracker.WasmSampleApp
dotnet run
```

### 2. Test WASM Features
1. **Navigate to**: `https://localhost:5001/whydidyourender-demo`
2. **Check Browser Console**: Should see WhyDidYouRender initialization and render events
3. **Check Browser Storage**: Should see session data in localStorage/sessionStorage
4. **Test Interactions**:
   - Click "Click me (+1)" - should log render events to browser console
   - Click "Update Time Only" - should track parameter changes
   - Click "Force Re-render" - should show unnecessary re-render warnings
   - Toggle child component visibility - should track component lifecycle

### 3. Expected WASM Behavior
- ✅ Browser console shows all render tracking
- ✅ Browser storage contains session data
- ✅ JavaScript interop works for performance tracking
- ✅ Component inheritance works with TrackedComponentBase
- ✅ No server console output (client-side only)

## 🔄 Cross-Platform Verification

### 1. Feature Parity Check
| Feature | Server | WASM | Status |
|---------|--------|------|--------|
| Component tracking | ✅ | ✅ | ✅ |
| Parameter change detection | ✅ | ✅ | ✅ |
| Performance metrics | ✅ | ✅ | ✅ |
| Session management | HttpContext | Browser Storage | ✅ |
| Console logging | Server + Browser | Browser Only | ✅ |
| TrackedComponentBase | ✅ | ✅ | ✅ |

### 2. Environment Detection
- **Server**: Should detect as `BlazorHostingModel.Server`
- **WASM**: Should detect as `BlazorHostingModel.WebAssembly`
- **Services**: Should use appropriate implementations automatically

### 3. Configuration Adaptation
- **Server**: Can use all output types (Console, BrowserConsole, Both)
- **WASM**: Automatically adapts Console output to BrowserConsole

## 🐛 Troubleshooting

### Common Issues

#### 1. WASM Build Fails
- **Error**: `NETSDK1082: There was no runtime pack for Microsoft.AspNetCore.App`
- **Solution**: Verify framework reference is conditional in main library

#### 2. Missing Console Output
- **Server**: Check `TrackingOutput` configuration
- **WASM**: Ensure browser console is open and configured correctly

#### 3. Session Management Issues
- **Server**: Verify session middleware is enabled
- **WASM**: Check browser storage permissions

#### 4. Component Not Tracked
- **Solution**: Ensure component inherits from `TrackedComponentBase`

### Debug Commands

```bash
# Check build status
dotnet build Blazor.WhyDidYouRender.csproj
dotnet build RenderTracker.SampleApp/RenderTracker.SampleApp.csproj
dotnet build RenderTracker.WasmSampleApp/RenderTracker.WasmSampleApp.csproj

# Run with verbose logging
dotnet run --verbosity detailed

# Check package references
dotnet list package
```

## ✅ Success Criteria

### Phase 6.4 Complete When:
- [ ] Server sample app runs without errors
- [ ] WASM sample app runs without errors
- [ ] Both environments show render tracking
- [ ] Cross-platform components work identically
- [ ] Environment detection works correctly
- [ ] Session management works in both environments
- [ ] Performance tracking works in both environments

### Final Validation:
- [ ] No compilation errors in any project
- [ ] No runtime errors in either environment
- [ ] Feature parity between Server and WASM
- [ ] Documentation reflects current functionality
- [ ] All demo components work as expected
