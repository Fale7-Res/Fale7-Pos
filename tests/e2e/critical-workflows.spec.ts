import {test,expect,Page} from '@playwright/test';

const ENTERPRISE_KEY='falah-enterprise-domains-v1';
const CORE_KEY='fateh-restaurant-os-v2';
const emptyReconciliation={status:'balanced',sales:{grossSalesPiastres:0,refundsPiastres:0,netSalesPiastres:0},payments:{cashPiastres:0,cardPiastres:0,instapayPiastres:0},expenses:{totalPiastres:0}};

function rejectRuntimeErrors(page:Page){
  const failures:string[]=[];
  page.on('pageerror',error=>failures.push(`pageerror: ${error.message}`));
  page.on('console',message=>{const location=message.location().url;if(message.type()==='error'&&!(message.text().includes('404')&&location.includes('favicon')))failures.push(`console.error: ${message.text()} ${location}`)});
  return ()=>expect(failures,'unexpected browser runtime errors').toEqual([]);
}

async function unlock(page:Page){for(const digit of ['1','2','3','4'])await page.getByRole('button',{name:digit,exact:true}).click();await page.locator('button.bg-red-600').click();await expect(page.locator('nav')).toBeVisible()}
async function openModule(page:Page,id:string){await page.locator(`[data-module="${id}"]`).click()}
async function loadWithEnterprise(page:Page,value:unknown){await page.addInitScript(({key,value})=>{localStorage.clear();localStorage.setItem(key,JSON.stringify(value))},{key:ENTERPRISE_KEY,value});await page.goto('/')}

test('E2E-START-001 fresh browser state loads without uncaught exception',async({page})=>{const clean=rejectRuntimeErrors(page);await page.goto('/');await page.evaluate(()=>localStorage.clear());await page.reload();await expect(page.getByRole('button',{name:'1',exact:true})).toBeVisible();clean()});
test('E2E-START-002 legacy enterprise LocalStorage loads without crash',async({page})=>{const clean=rejectRuntimeErrors(page);await loadWithEnterprise(page,{treasuryMovements:[],treasuryAccounts:[],inventoryItems:[],businessDays:[]});await unlock(page);await openModule(page,'delivery');await expect(page.locator('main')).toBeVisible();clean()});
test('E2E-START-003 clean context has no project hydration error',async({page})=>{const clean=rejectRuntimeErrors(page);await page.goto('/');await expect(page.locator('html')).toHaveAttribute('dir','rtl');clean()});
test('E2E-BD-001 no Business Day renders settlement safely',async({page})=>{const clean=rejectRuntimeErrors(page);await loadWithEnterprise(page,{treasuryMovements:[],treasuryAccounts:[],inventoryItems:[],businessDays:[]});await unlock(page);await openModule(page,'settlement');await expect(page.locator('[data-testid="open-business-day"]')).toBeVisible();clean()});
test('E2E-BD-002 open Business Day renders safely',async({page})=>{const clean=rejectRuntimeErrors(page);await loadWithEnterprise(page,{treasuryMovements:[],treasuryAccounts:[],inventoryItems:[],businessDays:[{id:'day-open',date:'2026-08-21',status:'open'}]});await unlock(page);await openModule(page,'settlement');await expect(page.locator('[data-testid="open-business-day"]')).toHaveCount(0);clean()});
test('E2E-BD-003 closed old-format day and snapshot render safely',async({page})=>{const clean=rejectRuntimeErrors(page);await loadWithEnterprise(page,{treasuryMovements:[],treasuryAccounts:[],inventoryItems:[],businessDays:[{id:'day-closed',date:'2026-08-20',status:'closed',snapshotId:'snap-old'}],dailyCloseSnapshots:[{id:'snap-old',businessDayId:'day-closed',businessDate:'2026-08-20',closedBy:'owner',operationId:'close-old',reconciliation:emptyReconciliation}]});await unlock(page);await openModule(page,'settlement');await expect(page.locator('[data-testid="reopen-business-day"]')).toBeVisible();clean()});
test('E2E-DEL-001 delivery empty state renders safely',async({page})=>{const clean=rejectRuntimeErrors(page);await loadWithEnterprise(page,{treasuryMovements:[],treasuryAccounts:[],inventoryItems:[],businessDays:[],drivers:[],deliveries:[],deliveryTrips:[],driverCustodies:[]});await unlock(page);await openModule(page,'delivery');await expect(page.locator('main')).toBeVisible();clean()});
test('E2E-CTRL-001 control center empty sources render safely',async({page})=>{const clean=rejectRuntimeErrors(page);await loadWithEnterprise(page,{treasuryMovements:[],treasuryAccounts:[],inventoryItems:[],businessDays:[],driverCustodies:[],deliveryTrips:[],deliveries:[],driverSettlements:[],exceptionStates:{}});await unlock(page);await openModule(page,'control_center');await expect(page.locator('main')).toBeVisible();clean()});
test('E2E-PERSIST-001 corrupt core JSON shows controlled recovery',async({page})=>{const clean=rejectRuntimeErrors(page);await page.addInitScript(key=>{localStorage.clear();localStorage.setItem(key,'{broken')},CORE_KEY);await page.goto('/');await expect(page.getByText('تم إيقاف العمليات لحماية البيانات')).toBeVisible();clean()});
test('E2E-PERSIST-002 corrupt enterprise JSON shows controlled recovery',async({page})=>{const clean=rejectRuntimeErrors(page);await page.addInitScript(key=>{localStorage.clear();localStorage.setItem(key,'{broken')},ENTERPRISE_KEY);await page.goto('/');await expect(page.getByText('تم إيقاف العمليات لحماية البيانات')).toBeVisible();clean()});
