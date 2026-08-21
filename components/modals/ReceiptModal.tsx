'use client';

import React, { useEffect, useState } from 'react';
import { useAppStore } from '@/lib/store';
import { Printer, X, Check, Flame, Receipt, FileText, Share2, Copy } from 'lucide-react';

export default function ReceiptModal() {
  const {
    receiptModalOpen,
    setReceiptModalOpen,
    activeReceiptOrder,
    activeReceiptType,
    activeShiftForReport,
    settings,
    currentUser,
    logAuditEvent,
  } = useAppStore();

  const [activeTab, setActiveTab] = useState<'customer' | 'grill' | 'shift'>(
    activeReceiptType === 'shift_close' ? 'shift' : activeReceiptType === 'grill' ? 'grill' : 'customer'
  );
  const [copied, setCopied] = useState(false);

  useEffect(() => {
    if (receiptModalOpen) {
      // Reset the printable view whenever a new receipt is opened.
      // eslint-disable-next-line react-hooks/set-state-in-effect
      setActiveTab(activeReceiptType === 'shift_close' ? 'shift' : activeReceiptType === 'grill' ? 'grill' : 'customer');
    }
  }, [receiptModalOpen, activeReceiptType, activeReceiptOrder?.id]);

  if (!receiptModalOpen) return null;

  const handlePrint = () => {
    window.print();
    logAuditEvent({
      userId: currentUser.id,
      userName: currentUser.name,
      userRole: currentUser.role,
      action: `طباعة ${activeTab === 'customer' ? 'إيصال العميل' : activeTab === 'grill' ? 'بون التحضير' : 'تقرير الشيفت'}`,
      category: activeTab === 'shift' ? 'shift' : 'order',
      details: `تم إرسال المستند للطباعة${activeReceiptOrder ? ` للطلب ${activeReceiptOrder.orderNumber}` : ''}`,
      severity: 'info',
      shiftId: activeReceiptOrder?.shiftId || activeShiftForReport?.id,
      device: 'PRN-01',
    });
  };

  const handleCopyText = async () => {
    const text = document.getElementById('printable-receipt')?.innerText || '';
    await navigator.clipboard?.writeText(text);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  };

  const handleDualPrint = () => {
    setActiveTab('customer');
    window.setTimeout(() => {
      window.print();
      if (activeReceiptOrder?.hasGrillItems) {
        setActiveTab('grill');
        window.setTimeout(() => window.print(), 250);
      }
    }, 150);
    logAuditEvent({
      userId: currentUser.id, userName: currentUser.name, userRole: currentUser.role,
      action: `طباعة نسختي الطلب ${activeReceiptOrder?.orderNumber || ''}`,
      category: 'order', details: 'إيصال عميل + بون تحضير مستقل', severity: 'info',
      shiftId: activeReceiptOrder?.shiftId, device: 'PRN-01',
    });
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/70 backdrop-blur-xs p-4 animate-in fade-in duration-150">
      <div className="bg-slate-900 text-white rounded-2xl shadow-2xl max-w-2xl w-full overflow-hidden border border-slate-700 flex flex-col max-h-[90vh]">
        {/* Modal Header */}
        <div className="bg-slate-800 px-6 py-4 border-b border-slate-700 flex items-center justify-between">
          <div className="flex items-center gap-3">
            <div className="p-2 bg-slate-700 rounded-lg text-emerald-400">
              <Printer className="w-5 h-5" />
            </div>
            <div>
              <h3 className="font-bold text-lg">معاينة الطباعة الحرارية (80mm)</h3>
              <p className="text-xs text-slate-400">محاكاة الطباعة المباشرة على طابعة الفواتير Xprinter</p>
            </div>
          </div>
          <button
            onClick={() => setReceiptModalOpen(false)}
            className="p-1.5 text-slate-400 hover:text-white hover:bg-slate-700 rounded-lg transition-colors"
          >
            <X className="w-5 h-5" />
          </button>
        </div>

        {/* Tab Selection */}
        <div className="bg-slate-800/80 px-6 py-2 border-b border-slate-700 flex items-center gap-2">
          {activeReceiptType !== 'shift_close' && (
            <>
              <button
                type="button"
                onClick={() => setActiveTab('customer')}
                className={`px-4 py-2 text-sm font-semibold rounded-lg transition-colors flex items-center gap-2 ${
                  activeTab === 'customer'
                    ? 'bg-red-600 text-white shadow-xs'
                    : 'text-slate-400 hover:text-white hover:bg-slate-700'
                }`}
              >
                <Receipt className="w-4 h-4" />
                إيصال العميل
              </button>
              {activeReceiptOrder?.hasGrillItems && (
                <button
                  type="button"
                  onClick={() => setActiveTab('grill')}
                  className={`px-4 py-2 text-sm font-semibold rounded-lg transition-colors flex items-center gap-2 ${
                    activeTab === 'grill'
                      ? 'bg-amber-600 text-white shadow-xs'
                      : 'text-slate-400 hover:text-white hover:bg-slate-700'
                  }`}
                >
                  <Flame className="w-4 h-4 text-amber-300" />
                  بون الفحم والتحضير
                </button>
              )}
            </>
          )}

          {activeReceiptType === 'shift_close' && (
            <button
              type="button"
              onClick={() => setActiveTab('shift')}
              className="px-4 py-2 text-sm font-semibold rounded-lg bg-emerald-600 text-white shadow-xs flex items-center gap-2"
            >
              <FileText className="w-4 h-4" />
              بون تقفيل الوردية
            </button>
          )}
        </div>

        {/* Modal Body - Thermal Paper Simulation */}
        <div className="flex-1 overflow-y-auto p-6 bg-slate-950/70 flex justify-center">
          {/* Thermal Receipt Container */}
          <div
            id="printable-receipt"
            className="w-[330px] bg-[#fffef7] text-slate-900 p-5 rounded-md shadow-2xl font-mono text-xs border border-amber-200/50 select-text leading-tight"
            dir="rtl"
          >
            {/* 1. Customer Receipt */}
            {activeTab === 'customer' && activeReceiptOrder && (
              <div className="space-y-3">
                {/* Header */}
                <div className="text-center pb-2 border-b border-dashed border-slate-400 space-y-1">
                  <h1 className="font-extrabold text-base tracking-wide font-sans">{settings.restaurantName}</h1>
                  <p className="text-[11px] font-bold text-slate-700">{settings.subName}</p>
                  <p className="text-[10px] text-slate-600">{settings.address}</p>
                  <p className="text-[10px] text-slate-600" dir="ltr">
                    {settings.phone1} - {settings.phone2}
                  </p>
                  <p className="text-[10px] text-slate-600">شكاوى: {settings.complaintsPhone}</p>
                </div>

                {/* Order Meta */}
                <div className="py-1 border-b border-dashed border-slate-400 text-[11px] space-y-1">
                  <div className="flex justify-between items-center">
                    <span className="font-extrabold text-sm text-black">رقم الطلب:</span>
                    <span className="font-extrabold text-base bg-black text-white px-2 py-0.5 rounded-sm">
                      {activeReceiptOrder.orderNumber}
                    </span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-slate-600">التاريخ والوقت:</span>
                    <span>{activeReceiptOrder.createdAt}</span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-slate-600">الكاشير:</span>
                    <span>{activeReceiptOrder.cashierName}</span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-slate-600">نوع الطلب:</span>
                    <span className="font-bold">
                      {activeReceiptOrder.orderType === 'dine_in'
                        ? `صالة (${activeReceiptOrder.tableNumber || 'طاولة'})`
                        : activeReceiptOrder.orderType === 'takeaway'
                        ? 'تيك أواي'
                        : `دليفري (${activeReceiptOrder.customerName || ''})`}
                    </span>
                  </div>
                  {activeReceiptOrder.deliveryAddress && (
                    <div className="text-[10px] text-slate-700 pt-0.5">
                      <span className="font-semibold">العنوان: </span>
                      {activeReceiptOrder.deliveryAddress}
                    </div>
                  )}
                </div>

                {/* Items Table */}
                <div className="py-2 border-b border-dashed border-slate-400">
                  <table className="w-full text-right text-[11px]">
                    <thead>
                      <tr className="border-b border-slate-300 font-bold">
                        <th className="pb-1">الصنف</th>
                        <th className="pb-1 text-center">الكمية</th>
                        <th className="pb-1 text-left">الإجمالي</th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-slate-200">
                      {activeReceiptOrder.items.map((item, idx) => (
                        <tr key={idx} className="py-1">
                          <td className="py-1.5 font-medium">
                            <div>{item.productName}</div>
                            {item.variantName && (
                              <div className="text-[10px] text-slate-500 font-normal">[{item.variantName}]</div>
                            )}
                            {item.notes && (
                              <div className="text-[10px] text-amber-800 font-normal">({item.notes})</div>
                            )}
                          </td>
                          <td className="py-1.5 text-center font-bold">{item.quantity}</td>
                          <td className="py-1.5 text-left font-bold">{item.totalPrice} ج</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>

                {/* Totals */}
                <div className="space-y-1.5 text-[11px] py-1 border-b border-dashed border-slate-400">
                  <div className="flex justify-between">
                    <span className="text-slate-600">المجموع الفرعي:</span>
                    <span>{activeReceiptOrder.subtotal} ج</span>
                  </div>
                  {activeReceiptOrder.discount > 0 && (
                    <div className="flex justify-between text-red-600 font-semibold">
                      <span>الخصم:</span>
                      <span>- {activeReceiptOrder.discount} ج</span>
                    </div>
                  )}
                  {(activeReceiptOrder.taxAmount || 0) > 0 && (
                    <div className="flex justify-between text-slate-600">
                      <span>{activeReceiptOrder.taxRuleName || 'الضريبة'} ({(activeReceiptOrder.taxRateBasisPoints || 0) / 100}%):</span>
                      <span>{activeReceiptOrder.taxMode === 'inclusive' ? 'شاملة ' : '+'}{activeReceiptOrder.taxAmount} ج</span>
                    </div>
                  )}
                  {activeReceiptOrder.deliveryFee && activeReceiptOrder.deliveryFee > 0 && (
                    <div className="flex justify-between text-slate-600">
                      <span>خدمة التوصيل:</span>
                      <span>+{activeReceiptOrder.deliveryFee} ج</span>
                    </div>
                  )}
                  <div className="flex justify-between items-center font-extrabold text-sm pt-1 border-t border-slate-300">
                    <span>الإجمالي النهائي:</span>
                    <span className="text-base text-black">{activeReceiptOrder.total} ج.م</span>
                  </div>
                  <div className="flex justify-between text-slate-700 pt-1">
                    <span>طريقة الدفع:</span>
                    <span className="font-bold">
                      {activeReceiptOrder.paymentMethod === 'cash'
                        ? 'نقداً (كاش)'
                        : activeReceiptOrder.paymentMethod === 'instapay'
                        ? 'تحويل InstaPay'
                        : 'بطاقة بنكية (فيزا)'}
                    </span>
                  </div>
                  {activeReceiptOrder.paymentMethod === 'cash' && (
                    <>
                      <div className="flex justify-between text-slate-600">
                        <span>المدفوع:</span>
                        <span>{activeReceiptOrder.amountPaid} ج</span>
                      </div>
                      <div className="flex justify-between text-slate-800 font-bold">
                        <span>المتبقي (الباقي):</span>
                        <span>{activeReceiptOrder.changeDue} ج</span>
                      </div>
                    </>
                  )}
                </div>

                {/* Footer Message */}
                <div className="text-center pt-2 text-[10px] text-slate-600 space-y-1 font-sans">
                  <p className="font-bold">{settings.receiptFooter}</p>
                  <p className="text-[9px] text-slate-400">نظام فاتح أبو الغنية لإدارة المطاعم</p>
                </div>
              </div>
            )}

            {/* 2. Preparation / Grill Ticket (No Prices, HUGE Order Number) */}
            {activeTab === 'grill' && activeReceiptOrder && (
              <div className="space-y-3">
                <div className="text-center pb-2 border-b-2 border-dashed border-black">
                  <div className="text-xs font-extrabold tracking-wider text-slate-800">بون تجهيز المشويات والفحم</div>
                  <div className="text-4xl font-black my-2 font-mono tracking-wider bg-black text-white py-2 rounded-md">
                    {activeReceiptOrder.orderNumber}
                  </div>
                  <div className="text-xs font-bold flex justify-between px-2 text-slate-700">
                    <span>{activeReceiptOrder.createdAt}</span>
                    <span>
                      {activeReceiptOrder.orderType === 'dine_in'
                        ? `صالة (${activeReceiptOrder.tableNumber})`
                        : activeReceiptOrder.orderType === 'takeaway'
                        ? 'تيك أواي'
                        : 'دليفري'}
                    </span>
                  </div>
                </div>

                <div className="py-2 border-b-2 border-dashed border-black">
                  <div className="text-xs font-bold text-slate-500 mb-2">الأصناف المطلوبة للتجهيز:</div>
                  <div className="space-y-3 text-sm">
                    {activeReceiptOrder.items
                      .filter((item) => item.sendToGrill)
                      .map((item, idx) => (
                        <div key={idx} className="flex justify-between items-start bg-slate-100 p-2 rounded-sm border border-slate-300">
                          <div className="pr-1">
                            <div className="font-black text-base">{item.productName}</div>
                            {item.variantName && (
                              <div className="text-xs font-bold text-red-600 mt-0.5">نوع الخبز/الحجم: {item.variantName}</div>
                            )}
                            {item.notes && (
                              <div className="text-xs font-bold text-amber-900 bg-amber-100 px-1.5 py-0.5 rounded-sm mt-1">
                                ⚠️ ملاحظة: {item.notes}
                              </div>
                            )}
                          </div>
                          <div className="text-2xl font-black bg-black text-white w-8 h-8 rounded-full flex items-center justify-center shrink-0">
                            {item.quantity}
                          </div>
                        </div>
                      ))}
                  </div>
                </div>

                <div className="text-center text-[11px] text-slate-600 font-bold pt-1">
                  *** يُسلم هذا البون للشيف المسؤول عن الشواية ***
                </div>
              </div>
            )}

            {/* 3. Shift Closing Settlement Report */}
            {activeTab === 'shift' && activeShiftForReport && (
              <div className="space-y-3">
                <div className="text-center pb-2 border-b border-dashed border-slate-400 space-y-1">
                  <h1 className="font-extrabold text-base tracking-wide font-sans">{settings.restaurantName}</h1>
                  <p className="text-xs font-bold text-slate-800">تقرير تسوية وتقفيل الوردية</p>
                  <p className="text-[11px] text-slate-600">{activeShiftForReport.shiftName}</p>
                </div>

                <div className="py-1 border-b border-dashed border-slate-400 text-[11px] space-y-1">
                  <div className="flex justify-between">
                    <span className="text-slate-600">تاريخ الفتح:</span>
                    <span>{activeShiftForReport.openedAt}</span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-slate-600">تاريخ الإغلاق:</span>
                    <span>{activeShiftForReport.closedAt || 'الآن'}</span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-slate-600">الكاشير المغلق:</span>
                    <span>{activeShiftForReport.closedByUserName || activeShiftForReport.openedByUserName}</span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-slate-600">عدد الطلبات:</span>
                    <span className="font-bold">{activeShiftForReport.ordersCount} طلب</span>
                  </div>
                </div>

                {/* Financial Summary */}
                <div className="py-2 border-b border-dashed border-slate-400 space-y-1.5 text-[11px]">
                  <div className="flex justify-between">
                    <span className="text-slate-600">رصيد الافتتاح:</span>
                    <span>{activeShiftForReport.openingCash} ج</span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-slate-600">إجمالي المبيعات:</span>
                    <span className="font-bold">{activeShiftForReport.totalSales} ج</span>
                  </div>
                  <div className="flex justify-between text-slate-700 pr-2">
                    <span>- مبيعات نقدية (كاش):</span>
                    <span>{activeShiftForReport.cashSales} ج</span>
                  </div>
                  <div className="flex justify-between text-slate-700 pr-2">
                    <span>- مبيعات بطاقات (فيزا):</span>
                    <span>{activeShiftForReport.cardSales} ج</span>
                  </div>
                  <div className="flex justify-between text-violet-700 pr-2">
                    <span>- مبيعات InstaPay:</span>
                    <span>{activeShiftForReport.instapaySales} ج</span>
                  </div>
                  <div className="flex justify-between text-red-600">
                    <span>إجمالي المصروفات:</span>
                    <span>- {activeShiftForReport.totalExpenses} ج</span>
                  </div>
                  {activeShiftForReport.totalRefunds > 0 && (
                    <div className="flex justify-between text-red-600">
                      <span>إجمالي المرتجعات:</span>
                      <span>- {activeShiftForReport.totalRefunds} ج</span>
                    </div>
                  )}
                </div>

                {/* Cash Drawer Count & Discrepancy */}
                <div className="py-2 border-b border-dashed border-slate-400 space-y-1.5 text-xs">
                  <div className="flex justify-between font-bold">
                    <span>الكاش المتوقع بالدرج:</span>
                    <span>{activeShiftForReport.expectedCash || 0} ج.م</span>
                  </div>
                  <div className="flex justify-between font-bold text-slate-900">
                    <span>الكاش الفعلي المعدود:</span>
                    <span>{activeShiftForReport.actualCash || 0} ج.م</span>
                  </div>
                  <div className="flex justify-between items-center font-extrabold text-sm pt-1 border-t border-slate-300">
                    <span>الفارق النقدي:</span>
                    <span
                      className={`px-2 py-0.5 rounded-sm ${
                        (activeShiftForReport.cashDifference || 0) === 0
                          ? 'bg-emerald-100 text-emerald-800'
                          : (activeShiftForReport.cashDifference || 0) < 0
                          ? 'bg-red-100 text-red-800'
                          : 'bg-amber-100 text-amber-800'
                      }`}
                    >
                      {(activeShiftForReport.cashDifference || 0) === 0
                        ? 'مطابق تماماً (0 ج)'
                        : (activeShiftForReport.cashDifference || 0) < 0
                        ? `عجز ${Math.abs(activeShiftForReport.cashDifference || 0)} ج`
                        : `زيادة ${activeShiftForReport.cashDifference} ج`}
                    </span>
                  </div>
                  {activeShiftForReport.differenceReason && (
                    <div className="text-[10px] text-red-700 bg-red-50 p-1.5 rounded-sm mt-1">
                      <span className="font-bold">سبب الفارق: </span>
                      {activeShiftForReport.differenceReason}
                    </div>
                  )}
                </div>

                {/* Signatures */}
                <div className="pt-3 flex justify-between text-[10px] text-slate-600">
                  <div className="text-center">
                    <p className="border-b border-slate-400 pb-4 mb-1">توقيع الكاشير</p>
                    <p>{activeShiftForReport.closedByUserName}</p>
                  </div>
                  <div className="text-center">
                    <p className="border-b border-slate-400 pb-4 mb-1">اعتماد المدير</p>
                    <p>{activeShiftForReport.differenceAuthorizedBy || 'سامح عبد الله'}</p>
                  </div>
                </div>
              </div>
            )}
          </div>
        </div>

        {/* Modal Footer Controls */}
        <div className="bg-slate-800 px-6 py-4 border-t border-slate-700 flex items-center justify-between">
          <div className="flex items-center gap-2 text-xs text-slate-400">
            <span className="w-2 h-2 rounded-full bg-emerald-400 animate-pulse"></span>
            <span>جاهز للإرسال إلى طابعة الفواتير (80mm)</span>
          </div>

          <div className="flex gap-3">
            <button
              type="button"
              onClick={handleCopyText}
              className="px-4 py-2 text-sm font-semibold bg-slate-700 hover:bg-slate-600 text-white rounded-xl transition-colors flex items-center gap-2"
            >
              {copied ? <Check className="w-4 h-4 text-emerald-400" /> : <Copy className="w-4 h-4" />}
              {copied ? 'تم النسخ' : 'نسخ النص'}
            </button>
            <button
              type="button"
              onClick={handlePrint}
              className="px-5 py-2 text-sm font-bold bg-emerald-600 hover:bg-emerald-700 active:bg-emerald-800 text-white rounded-xl shadow-md transition-colors flex items-center gap-2"
            >
              <Printer className="w-4 h-4" />
              طباعة فورية
            </button>
            {activeReceiptType === 'both' && activeReceiptOrder && (
              <button type="button" onClick={handleDualPrint} className="px-5 py-2 text-sm font-bold bg-red-600 hover:bg-red-700 text-white rounded-xl shadow-md flex items-center gap-2">
                <Printer className="w-4 h-4" /> طباعة النسختين
              </button>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}
