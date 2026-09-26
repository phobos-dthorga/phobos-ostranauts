# Inspect compiled UI wiring and native entry points; this does not execute Unity.
param([Parameter(Mandatory)]$Module,[Parameter(Mandatory)]$NativeModule)
$ErrorActionPreference = 'Stop'
function Instructions($Type) {
    @($Type.Methods | Where-Object HasBody | ForEach-Object { $_.Body.Instructions })
}
function NestedTypes($Type) {
    $Type
    foreach ($nested in $Type.NestedTypes) { NestedTypes $nested }
}
$picker = $Module.GetType('Phobos.Ostranauts.Framework.Controls.ObjectPicker')
$calls = @(NestedTypes $picker | ForEach-Object { Instructions $_ } | ForEach-Object Operand | Where-Object { $_ -is [Mono.Cecil.MethodReference] })
foreach ($forbidden in @('ShowInputSelector','HideInputSelector','SetBracketTarget','QueueInteraction','AIIssueOrder','SetInput','set_Highlight','set_DimLights','set_layer')) {
    if ($calls.Name -contains $forbidden) { throw "Ship picker invokes gameplay/native shared selection state: $forbidden" }
}
if ($calls.Name -notcontains 'GetMouseOverCOsExternal') { throw 'Picker lost native hit testing.' }
foreach ($method in @('MouseHandler','KeyHandler','GetMouseOverCOsExternal','CamZoom')) {
    if ($NativeModule.GetType('CrewSim').Methods.Name -notcontains $method) { throw "Missing native picker entry point: $method" }
}
foreach ($patch in @('PickerMouseIsolation','PickerKeyIsolation','PickerCommandIsolation')) {
    if ($null -eq $Module.GetType("Phobos.Ostranauts.Framework.Controls.$patch")) { throw "Missing input capture: $patch" }
}
$command = $NativeModule.GetType('Ostranauts.InputControl.Command')
if ($command.Methods.Name -notcontains 'Perform' -or $null -eq $NativeModule.GetType('Ostranauts.InputControl.CommandUICancel')) { throw 'Native command capture/Cancel contract changed.' }
$allow = $picker.Methods | Where-Object Name -eq 'AllowCommand'
if (@($allow.Body.Instructions | ForEach-Object Operand | Where-Object { $_ -is [Mono.Cecil.MethodReference] -and $_.Name -eq 'Cancel' }).Count -ne 1) { throw 'Escape must cancel only the active picker.' }
$crew = $Module.GetType('Phobos.Ostranauts.Framework.Crew.CrewPanel')
$toggles = @(NestedTypes $crew | ForEach-Object { $_.Methods } | Where-Object {
    $_.HasBody -and $_.Name -like '<RenderOrder>b__*' -and @($_.Body.Instructions | ForEach-Object Operand | Where-Object { $_ -is [Mono.Cecil.MethodReference] -and $_.Name -eq 'SetActive' }).Count -gt 0
})
if ($toggles.Count -ne 1) { throw 'Expected one reusable diagnostics disclosure callback.' }
if (@($toggles[0].Body.Instructions | ForEach-Object Operand | Where-Object { $_ -is [Mono.Cecil.MethodReference] -and $_.Name -in @('Label','Rect','AddComponent','Instantiate') }).Count -gt 0) { throw 'Repeated diagnostics clicks must not allocate more UI content.' }
$widgets = $Module.GetType('Phobos.Ostranauts.Framework.Controls.ConsoleWidgets')
$row = $widgets.Methods | Where-Object Name -eq 'Row'
$expand = @($row.Body.Instructions | Where-Object { $_.Operand -is [Mono.Cecil.MethodReference] -and $_.Operand.Name -eq 'set_childForceExpandWidth' })
if ($expand.Count -ne 1 -or $expand[0].Previous.OpCode.Code.ToString() -ne 'Ldc_I4_0') { throw 'Compact rows must respect fixed equipment pictures and step buttons.' }
$stepper = $widgets.Methods | Where-Object Name -eq 'Stepper'
if (@($stepper.Body.Instructions | ForEach-Object Operand | Where-Object { $_ -is [Mono.Cecil.MethodReference] -and $_.Name -eq 'StepIcon' }).Count -ne 2) { throw 'Both step symbols must be drawn independently of font coverage.' }
Write-Output 'PASS: compiled console wiring, stable diagnostics, fixed widths, drawn steppers, native hit-testing and command-isolation contracts. Not Unity execution.'
