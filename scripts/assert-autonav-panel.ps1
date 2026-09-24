# Inspect compiled references, without loading the plugin into Unity.
# The caller owns/disposes this Mono.Cecil.ModuleDefinition.
param([Parameter(Mandatory)]$Module)
$ErrorActionPreference = 'Stop'
$panel = $Module.GetType('PhobosAutoNav.AutoNavPanel')
if ($null -eq $panel) { throw 'Auto Nav panel type is missing.' }
$instructions = @($panel.Methods | Where-Object HasBody | ForEach-Object { $_.Body.Instructions })
$componentTypes = @($instructions | ForEach-Object {
    $method = $_.Operand
    if ($method -is [Mono.Cecil.GenericInstanceMethod] -and $method.Name -eq 'AddComponent') {
        $method.GenericArguments | ForEach-Object FullName
    }
})
$expected = 'Ostranauts.ShipGUIs.NavStation.Draggable'
if ($componentTypes -contains 'Draggable' -or $componentTypes -notcontains $expected) {
    throw 'Auto Nav must add the navigation-panel Draggable, not the global object-hauling Draggable.'
}
$binding = @($instructions | Where-Object {
    $_.OpCode.Code.ToString() -eq 'Stfld' -and $_.Operand -is [Mono.Cecil.FieldReference] -and
    $_.Operand.FullName -eq "$expected Ostranauts.ShipGUIs.NavStation.NavModBase::DraggableRef"
})
if ($binding.Count -ne 1) { throw 'Auto Nav must bind its navigation drag handler to NavModBase.DraggableRef.' }
if ($componentTypes -contains 'UnityEngine.UI.AspectRatioFitter') {
    throw 'Auto Nav must size its native placement bounds, not letterbox its artwork in a larger container.'
}
$boundsPatch = $Module.GetType('PhobosAutoNav.PanelBoundsPatch')
if ($null -eq $boundsPatch) { throw 'Auto Nav saved/default panel bounds must be normalized before native fit checks.' }
$normalizationCalls = @($boundsPatch.Methods | Where-Object HasBody | ForEach-Object { $_.Body.Instructions } | Where-Object {
    $_.Operand -is [Mono.Cecil.MethodReference] -and
    $_.Operand.FullName -eq 'System.Void PhobosAutoNav.AutoNavPanel::NormalizePlacementBounds()'
})
if ($normalizationCalls.Count -ne 1) { throw 'Auto Nav native fit hook must normalize only its own panel bounds.' }
