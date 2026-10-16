from pathlib import Path
import json
root=Path.cwd(); target=root/'Codex Work/Sub-Agents/Results/ui-findings.json'; data=json.loads(target.read_text(encoding='utf-8-sig'))
P='src/Falah.Client.CashierWinUI/'
def ev(path,needle,note,width=1):
 lines=(root/path).read_text(encoding='utf-8-sig').splitlines(); line=next(i+1 for i,t in enumerate(lines) if needle in t)
 return dict(path=path,startLine=line,endLine=line+width-1,explanation=note)
if not any(f['id']=='UI-021' for f in data['findings']):
 data['findings'].append(dict(id='UI-021',title='Late product-image completion changes save target beneath another product form',severity='HIGH',confidence='HIGH',status='SOURCE_CONFIRMED_STATIC_REASONING',baseline=data['baseline'],evidence=[
 ev(P+'ViewModels/MenuEditorViewModel.cs','public bool CanBrowse =>','Browse and edit ignore IsImageBusy.',2),
 ev(P+'ViewModels/MenuEditorViewModel.cs','[RelayCommand] private void SelectRow','Select allowed while image request awaits; changes editing identity and visible form.',16),
 ev(P+'ViewModels/MenuEditorViewModel.cs','[RelayCommand] private async Task ChooseImageAsync','Captures product before picker/upload awaits; no identity generation recheck.',30),
 ev(P+'ViewModels/MenuEditorViewModel.cs','_editing = product with { ImageVersion = null };','Remove has same stale identity overwrite.',4),
 ev(P+'ViewModels/MenuEditorViewModel.cs','return _editing is ProductDetailsDto p','Save request combines editing.Id with current form fields.',3),
 ev(P+'Views/MenuEditorView.xaml','Command="{Binding ElementName=Root, Path=DataContext.SelectRowCommand}"','Native catalogue selection command remains reachable.',2),
 ev(P+'Views/MenuEditorView.xaml','IsEnabled="{Binding CanEdit}" Command="{Binding SaveCommand}"','Save enabled by CanEdit, not image busy.')],
 trigger='Select product A; start delayed upload/delete; select B while request pending. A response restores _editing=A, while visible Name/Price/Variants remain B. After ReloadAsync finishes, save B-looking form.',
 rootCause='Image completion unconditionally overwrites shared editor identity captured before await. IsImageBusy only disables image controls; ReloadAsync reloads catalogs but does not reset/rebind visible form or editing identity.',
 impact='Save targets product A with B field values/variants. Can corrupt menu pricing, preparation route and recipe association despite valid authorized backend request. No product mutation executed by reviewer.',
 counterarguments=['Native file picker may prevent interaction while open; upload/delete await after picker is still vulnerable.','ReloadAsync temporarily disables form through IsLoading but leaves mismatched editor identity/form when complete.','Server can reject foreign variant IDs, reducing some cases; products without variants or compatible fields remain at risk.','Careful operator might notice photo mismatch; that does not restore correct target.'],
 regressionTests=['Controlled image TaskCompletionSource: A upload pending, SelectRow(B), complete A; editor target and form must remain B, A image catalog updated separately.','Repeat with delete, New, SetKind and Close/reopen; no stale completion alters current draft.','Assert saved product ID matches displayed selected item after every async completion.'],
 recommendation='Keep immutable operation target independent from current editor; only update visible state when selection generation still matches. Coordinate browse/save with image busy and invalidate late completions on close/kind/new.',runtimeReproduction='NOT_EXECUTED',screenshotEvidence='NOT_OBSERVED',independentChallenge='Parent/peer challenge requested; not yet incorporated'))
target.write_text(json.dumps(data,ensure_ascii=False,indent=2),encoding='utf-8')
print(len(data['findings']))
