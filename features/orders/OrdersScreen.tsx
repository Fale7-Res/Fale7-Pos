'use client';

import React, { useState } from 'react';
import { useAppStore } from '@/lib/store';
import { Order } from '@/lib/types';
import { createOperationId } from '@/lib/financial-posting';
import {
  Receipt,
  Search,
  Printer,
  XCircle,
  RotateCcw,
  Eye,
  Flame,
  CheckCircle2,
  AlertTriangle,
  Utensils,
  ShoppingBag,
  Truck,
  CreditCard,
  Banknote,
  Smartphone,
  X,
} from 'lucide-react';

export default function OrdersScreen() {
  const {
    orders,
    cancelPaidOrder,
    refundPaidOrder,
    showReceipt,
    requestManagerAuth,
    currentUser,
  } = useAppStore();

  const [searchQuery, setSearchQuery] = useState('');
  const [filterType, setFilterType] = useState<string>('all');
  const [filterStatus, setFilterStatus] = useState<string>('all');
  const [selectedOrder, setSelectedOrder] = useState<Order | null>(null);

  const filteredOrders = orders.filter((order) => {
    const matchesSearch =
      order.orderNumber.toLowerCase().includes(searchQuery.toLowerCase()) ||
      order.cashierName.toLowerCase().includes(searchQuery.toLowerCase()) ||
      (order.customerName && order.customerName.toLowerCase().includes(searchQuery.toLowerCase())) ||
      (order.customerPhone && order.customerPhone.includes(searchQuery));

    const matchesType = filterType === 'all' || order.orderType === filterType;
    const matchesStatus = filterStatus === 'all' || order.status === filterStatus;

    return matchesSearch && matchesType && matchesStatus;
  });

  // Cancel order flow
  const handleCancel = (order: Order) => {
    requestManagerAuth(
      `إلغاء الطلب المدفوع ${order.orderNumber}`,
      `قيمة الطلب: ${order.total} ج.م - الكاشير المنفذ: ${order.cashierName}`,
      true,
      (managerName, reason, approval) => {
        cancelPaidOrder(order.id, reason, managerName, approval);
        setSelectedOrder(null);
      },
      'orders_cancel'
    );
  };

  // Refund order flow
  const handleRefund = (order: Order) => {
    const remaining = Math.max(0, order.total - (order.refundAmount || 0));
    const refundRequestId = createOperationId('refund-request');
    requestManagerAuth(
      `استرجاع أموال الطلب ${order.orderNumber}`,
      `قيمة المرتجع: ${remaining} ج.م - الكاشير: ${order.cashierName}`,
      true,
      (managerName, reason, approval) => {
        refundPaidOrder(order.id, remaining, reason, managerName, refundRequestId, approval);
        setSelectedOrder(null);
      },
      'orders_refund'
    );
  };

  return (
    <div className="h-full overflow-y-auto bg-slate-100 p-6 space-y-6 font-sans select-none" dir="rtl">
      {/* 1. Header & Filters */}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 bg-white p-5 rounded-3xl border border-slate-200 shadow-xs">
        <div className="flex items-center gap-3">
          <div className="w-10 h-10 rounded-2xl bg-red-500/10 text-red-600 border border-red-500/20 flex items-center justify-center">
            <Receipt className="w-6 h-6" />
          </div>
          <div>
            <h1 className="text-xl font-black text-slate-900">سجل وبونات الطلبات</h1>
            <p className="text-xs text-slate-500">
              استعراض كافة الطلبات المنفذة، إعادة طباعة الإيصالات، والتحكم في الإلغاء والمرتجع بتفويض المدير
            </p>
          </div>
        </div>

        {/* Filters */}
        <div className="flex flex-wrap items-center gap-3">
          {/* Search */}
          <div className="relative">
            <Search className="w-4 h-4 text-slate-400 absolute right-3 top-1/2 -translate-y-1/2" />
            <input
              type="text"
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              placeholder="رقم البون، الكاشير، العميل..."
              className="bg-slate-100 text-xs pr-9 pl-3 py-2 rounded-xl border border-slate-200 focus:outline-hidden focus:border-red-500"
            />
          </div>

          {/* Type filter */}
          <select
            value={filterType}
            onChange={(e) => setFilterType(e.target.value)}
            className="bg-slate-100 text-xs px-3 py-2 rounded-xl border border-slate-200 font-bold text-slate-700"
          >
            <option value="all">كل الأنواع</option>
            <option value="dine_in">صالة</option>
            <option value="takeaway">تيك أواي</option>
            <option value="delivery">دليفري</option>
          </select>

          {/* Status filter */}
          <select
            value={filterStatus}
            onChange={(e) => setFilterStatus(e.target.value)}
            className="bg-slate-100 text-xs px-3 py-2 rounded-xl border border-slate-200 font-bold text-slate-700"
          >
            <option value="all">كل الحالات</option>
            <option value="completed">مكتمل</option>
            <option value="refunded">مسترجع</option>
            <option value="cancelled">ملغي</option>
          </select>
        </div>
      </div>

      {/* 2. Orders Table */}
      <div className="bg-white rounded-3xl border border-slate-200 shadow-xs overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full text-right text-xs">
            <thead>
              <tr className="bg-slate-100/70 border-b border-slate-200 text-slate-600 font-bold">
                <th className="py-3.5 px-4">رقم البون</th>
                <th className="py-3.5 px-4">الوقت والتاريخ</th>
                <th className="py-3.5 px-4">النوع</th>
                <th className="py-3.5 px-4">الكاشير</th>
                <th className="py-3.5 px-4">الأصناف المطلوبة</th>
                <th className="py-3.5 px-4 text-center">طريقة الدفع</th>
                <th className="py-3.5 px-4 text-center">الإجمالي</th>
                <th className="py-3.5 px-4 text-center">الحالة</th>
                <th className="py-3.5 px-4 text-center">الإجراءات</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {filteredOrders.map((order) => (
                <tr key={order.id} className="hover:bg-slate-50 transition-colors">
                  {/* Order Number */}
                  <td className="py-3.5 px-4 font-black text-slate-900 text-sm">
                    <div className="flex items-center gap-1.5">
                      <span>{order.orderNumber}</span>
                      {order.hasGrillItems && (
                        <span className="text-[10px] bg-amber-100 text-amber-800 px-1 py-0.2 rounded-sm font-bold flex items-center">
                          <Flame className="w-2.5 h-2.5" /> فحم
                        </span>
                      )}
                    </div>
                  </td>

                  {/* Date */}
                  <td className="py-3.5 px-4 text-slate-600 font-mono text-[11px]">{order.createdAt}</td>

                  {/* Order Type */}
                  <td className="py-3.5 px-4">
                    <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-md font-bold text-[11px] bg-slate-100 text-slate-800">
                      {order.orderType === 'dine_in' ? (
                        <>
                          <Utensils className="w-3 h-3 text-red-600" />
                          <span>صالة ({order.tableNumber || 'طاولة'})</span>
                        </>
                      ) : order.orderType === 'takeaway' ? (
                        <>
                          <ShoppingBag className="w-3 h-3 text-amber-600" />
                          <span>تيك أواي</span>
                        </>
                      ) : (
                        <>
                          <Truck className="w-3 h-3 text-blue-600" />
                          <span>دليفري</span>
                        </>
                      )}
                    </span>
                  </td>

                  {/* Cashier */}
                  <td className="py-3.5 px-4 font-medium text-slate-800">{order.cashierName}</td>

                  {/* Items summary */}
                  <td className="py-3.5 px-4 text-slate-700 max-w-xs truncate">
                    {order.items.map((i) => `${i.productName} (${i.quantity})`).join('، ')}
                  </td>

                  {/* Payment */}
                  <td className="py-3.5 px-4 text-center">
                    <span className="inline-flex items-center gap-1 text-[11px] font-bold text-slate-700">
                      {order.paymentMethod === 'cash' ? (
                        <Banknote className="w-3.5 h-3.5 text-emerald-600" />
                      ) : order.paymentMethod === 'instapay' ? (
                        <Smartphone className="w-3.5 h-3.5 text-violet-600" />
                      ) : (
                        <CreditCard className="w-3.5 h-3.5 text-blue-600" />
                      )}
                      <span>{order.paymentMethod === 'cash' ? 'كاش' : order.paymentMethod === 'instapay' ? 'InstaPay' : 'فيزا'}</span>
                    </span>
                  </td>

                  {/* Total */}
                  <td className="py-3.5 px-4 text-center font-black text-sm text-slate-900">
                    {order.total} ج.م
                  </td>

                  {/* Status */}
                  <td className="py-3.5 px-4 text-center">
                    <span
                      className={`inline-block px-2 py-0.5 rounded-full text-[10px] font-black ${
                        order.status === 'completed'
                          ? 'bg-emerald-100 text-emerald-800'
                          : order.status === 'refunded'
                          ? 'bg-purple-100 text-purple-800'
                          : 'bg-red-100 text-red-800'
                      }`}
                    >
                      {order.status === 'completed' ? 'مدفوع ومكتمل' : order.status === 'refunded' ? 'مسترجع' : 'ملغي'}
                    </span>
                  </td>

                  {/* Actions */}
                  <td className="py-3.5 px-4 text-center">
                    <div className="flex items-center justify-center gap-1">
                      <button
                        type="button"
                        onClick={() => setSelectedOrder(order)}
                        className="p-1.5 bg-slate-100 hover:bg-slate-200 text-slate-700 rounded-lg transition-colors"
                        title="عرض تفاصيل الطلب"
                      >
                        <Eye className="w-3.5 h-3.5" />
                      </button>
                      <button
                        type="button"
                        onClick={() => showReceipt(order, 'customer')}
                        className="p-1.5 bg-emerald-50 hover:bg-emerald-100 text-emerald-700 rounded-lg transition-colors"
                        title="طباعة إيصال العميل"
                      >
                        <Printer className="w-3.5 h-3.5" />
                      </button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>

      {/* 3. ORDER DETAILS MODAL */}
      {selectedOrder && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/70 backdrop-blur-xs p-4 animate-in fade-in">
          <div className="bg-white rounded-3xl shadow-2xl max-w-lg w-full overflow-hidden border border-slate-200 flex flex-col max-h-[90vh] text-slate-800">
            {/* Header */}
            <div className="bg-slate-900 text-white px-6 py-4 flex items-center justify-between">
              <div>
                <div className="flex items-center gap-2">
                  <h3 className="font-extrabold text-lg">تفاصيل البون {selectedOrder.orderNumber}</h3>
                  <span
                    className={`text-xs font-bold px-2 py-0.5 rounded-full ${
                      selectedOrder.status === 'completed'
                        ? 'bg-emerald-600 text-white'
                        : selectedOrder.status === 'refunded'
                        ? 'bg-purple-600 text-white'
                        : 'bg-red-600 text-white'
                    }`}
                  >
                    {selectedOrder.status === 'completed'
                      ? 'مكتمل'
                      : selectedOrder.status === 'refunded'
                      ? 'مسترجع'
                      : 'ملغي'}
                  </span>
                </div>
                <p className="text-xs text-slate-400 mt-0.5">
                  الكاشير: {selectedOrder.cashierName} • {selectedOrder.createdAt}
                </p>
              </div>
              <button
                type="button"
                onClick={() => setSelectedOrder(null)}
                className="p-1 text-slate-400 hover:text-white rounded-lg"
              >
                <X className="w-5 h-5" />
              </button>
            </div>

            {/* Order Items Table */}
            <div className="p-6 overflow-y-auto space-y-4">
              <div className="space-y-2">
                <h4 className="text-xs font-bold text-slate-600">الأصناف بالطلب:</h4>
                <div className="space-y-2">
                  {selectedOrder.items.map((item, idx) => (
                    <div
                      key={idx}
                      className="bg-slate-50 p-3 rounded-2xl border border-slate-200 flex justify-between items-center text-xs"
                    >
                      <div>
                        <div className="font-extrabold text-slate-900">{item.productName}</div>
                        {item.variantName && (
                          <div className="text-[11px] text-red-600 font-bold">الخيار: {item.variantName}</div>
                        )}
                        {item.notes && (
                          <div className="text-[11px] text-amber-800">ملاحظة: {item.notes}</div>
                        )}
                      </div>
                      <div className="text-left">
                        <span className="font-black text-sm">{item.totalPrice} ج</span>
                        <span className="text-[10px] text-slate-400 block font-normal">
                          ({item.quantity} × {item.unitPrice} ج)
                        </span>
                      </div>
                    </div>
                  ))}
                </div>
              </div>

              {/* Financial Breakdown */}
              <div className="bg-slate-50 p-3.5 rounded-2xl border border-slate-200 space-y-1.5 text-xs">
                <div className="flex justify-between text-slate-600">
                  <span>المجموع الفرعي:</span>
                  <span>{selectedOrder.subtotal} ج</span>
                </div>
                {selectedOrder.discount > 0 && (
                  <div className="flex justify-between text-red-600 font-bold">
                    <span>الخصم المطبق:</span>
                    <span>- {selectedOrder.discount} ج</span>
                  </div>
                )}
                {selectedOrder.deliveryFee && selectedOrder.deliveryFee > 0 && (
                  <div className="flex justify-between text-slate-600">
                    <span>خدمة التوصيل:</span>
                    <span>+{selectedOrder.deliveryFee} ج</span>
                  </div>
                )}
                <div className="flex justify-between items-center font-black text-sm pt-2 border-t border-slate-200">
                  <span>الإجمالي المدفوع:</span>
                  <span className="text-base text-red-600">{selectedOrder.total} ج.م</span>
                </div>
              </div>

              {/* Customer info if delivery */}
              {selectedOrder.customerName && (
                <div className="p-3 bg-blue-50 border border-blue-200 rounded-2xl text-xs text-blue-950 space-y-1">
                  <div className="font-bold">بيانات التوصيل والعميل:</div>
                  <div>العميل: {selectedOrder.customerName} ({selectedOrder.customerPhone})</div>
                  <div>العنوان: {selectedOrder.deliveryAddress}</div>
                </div>
              )}
            </div>

            {/* Modal Footer / Action Buttons */}
            <div className="bg-slate-50 px-6 py-4 border-t border-slate-200 flex items-center justify-between gap-2">
              <div className="flex gap-2">
                <button
                  type="button"
                  onClick={() => showReceipt(selectedOrder, 'customer')}
                  className="px-3 py-2 bg-emerald-600 hover:bg-emerald-700 text-white rounded-xl text-xs font-bold flex items-center gap-1.5"
                >
                  <Printer className="w-4 h-4" />
                  <span>طباعة الإيصال</span>
                </button>
                {selectedOrder.hasGrillItems && (
                  <button
                    type="button"
                    onClick={() => showReceipt(selectedOrder, 'grill')}
                    className="px-3 py-2 bg-amber-600 hover:bg-amber-700 text-white rounded-xl text-xs font-bold flex items-center gap-1.5"
                  >
                    <Flame className="w-4 h-4" />
                    <span>بون الفحم</span>
                  </button>
                )}
              </div>

              {selectedOrder.status === 'completed' && (
                <div className="flex gap-2">
                  <button
                    type="button"
                    onClick={() => handleRefund(selectedOrder)}
                    className="px-3 py-2 bg-purple-100 hover:bg-purple-200 text-purple-800 rounded-xl text-xs font-bold transition-colors flex items-center gap-1"
                  >
                    <RotateCcw className="w-3.5 h-3.5" />
                    <span>استرجاع (مرتجع)</span>
                  </button>
                  <button
                    type="button"
                    onClick={() => handleCancel(selectedOrder)}
                    className="px-3 py-2 bg-red-100 hover:bg-red-200 text-red-800 rounded-xl text-xs font-bold transition-colors flex items-center gap-1"
                  >
                    <XCircle className="w-3.5 h-3.5" />
                    <span>إلغاء الطلب</span>
                  </button>
                </div>
              )}
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
