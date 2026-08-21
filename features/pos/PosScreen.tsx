'use client';

import React, { useEffect, useMemo, useRef, useState } from 'react';
import Image from 'next/image';
import { useAppStore } from '@/lib/store';
import { Product, ProductVariant, OrderType, PaymentMethod } from '@/lib/types';
import { createOperationId } from '@/lib/financial-posting';
import {
  Search,
  Plus,
  Minus,
  Trash2,
  Percent,
  CreditCard,
  Smartphone,
  Banknote,
  Utensils,
  ShoppingBag,
  Truck,
  Flame,
  X,
  Check,
  RotateCcw,
  Sparkles,
  Layers,
  FileText,
  User,
  Phone,
  MapPin,
  Clock,
} from 'lucide-react';

export default function PosScreen() {
  const {
    categories,
    products,
    orders,
    cart,
    addToCart,
    updateCartItemQty,
    removeCartItem,
    updateCartItemNotes,
    clearCart,
    cartSubtotal,
    cartTotal,
    discount,
    setDiscount,
    orderType,
    setOrderType,
    tableNumber,
    setTableNumber,
    customerName,
    setCustomerName,
    customerPhone,
    setCustomerPhone,
    deliveryAddress,
    setDeliveryAddress,
    checkout,
    currentUser,
    requestManagerAuth,
    settings,
    activeShift,
    setActiveModule,
  } = useAppStore();

  const [selectedCategory, setSelectedCategory] = useState<string>(categories[0]?.id || 'cat-sandwiches-faleh');
  const [searchQuery, setSearchQuery] = useState<string>('');
  const searchInputRef = useRef<HTMLInputElement>(null);
  const [variantModalProduct, setVariantModalProduct] = useState<Product | null>(null);
  const [selectedVariant, setSelectedVariant] = useState<ProductVariant | null>(null);
  const [itemNotes, setItemNotes] = useState<string>('');

  // Checkout modal
  const [checkoutModalOpen, setCheckoutModalOpen] = useState<boolean>(false);
  const [paymentMethod, setPaymentMethod] = useState<PaymentMethod>('cash');
  const [amountPaidInput, setAmountPaidInput] = useState<string>('');
  const [checkoutError, setCheckoutError] = useState<string>('');
  const [isProcessingPayment, setIsProcessingPayment] = useState(false);
  const checkoutRequestIdRef = useRef<string | null>(null);

  // Discount modal
  const [discountModalOpen, setDiscountModalOpen] = useState<boolean>(false);
  const [discountInput, setDiscountInput] = useState<string>('');
  const [discountReasonInput, setDiscountReasonInput] = useState<string>('');

  // Filter products by category and search
  const filteredProducts = useMemo(() => {
    return products.filter((p) => {
      if (!p.isAvailable) return false;
      const matchesCat = searchQuery ? true : p.categoryId === selectedCategory;
      const matchesSearch = searchQuery
        ? p.name.toLowerCase().includes(searchQuery.toLowerCase())
        : true;
      return matchesCat && matchesSearch;
    });
  }, [products, selectedCategory, searchQuery]);

  // Click on product card
  const handleProductClick = (product: Product) => {
    if (product.variants && product.variants.length > 0) {
      setVariantModalProduct(product);
      setSelectedVariant(product.variants[0]);
      setItemNotes('');
    } else {
      addToCart(product, undefined, undefined);
    }
  };

  // Confirm variant selection
  const handleConfirmVariant = () => {
    if (variantModalProduct && selectedVariant) {
      addToCart(variantModalProduct, selectedVariant, itemNotes || undefined);
      setVariantModalProduct(null);
      setSelectedVariant(null);
      setItemNotes('');
    }
  };

  // Open Checkout
  const handleOpenCheckout = () => {
    if (cart.length === 0) return;
    const finalTotal = cartTotal + (orderType === 'delivery' ? settings.deliveryFee : 0);
    setAmountPaidInput(String(finalTotal));
    setPaymentMethod('cash');
    setCheckoutError('');
    checkoutRequestIdRef.current = createOperationId('checkout-request');
    setCheckoutModalOpen(true);
  };

  // Handle Quick Cash Buttons (e.g. 50, 100, 200, 500)
  const handleQuickCash = (amount: number) => {
    setAmountPaidInput(String(amount));
  };

  // Keypad for checkout
  const handleKeypadPress = (val: string) => {
    if (val === 'clear') {
      setAmountPaidInput('');
    } else if (val === 'back') {
      setAmountPaidInput((prev) => prev.slice(0, -1));
    } else {
      setAmountPaidInput((prev) => prev + val);
    }
  };

  // Perform Final Payment
  const handleFinalCheckout = () => {
    if (isProcessingPayment) return;
    const finalTotal = cartTotal + (orderType === 'delivery' ? settings.deliveryFee : 0);
    const paidNum = paymentMethod === 'cash' ? Number(amountPaidInput) || 0 : finalTotal;

    if (paymentMethod === 'cash' && paidNum < finalTotal) {
      setCheckoutError(`المبلغ المدفوع (${paidNum} ج) أقل من إجمالي الطلب (${finalTotal} ج).`);
      return;
    }

    try {
      setIsProcessingPayment(true);
      const requestId = checkoutRequestIdRef.current || createOperationId('checkout-request');
      checkoutRequestIdRef.current = requestId;
      checkout(paymentMethod, paidNum, requestId);
      checkoutRequestIdRef.current = null;
      setCheckoutModalOpen(false);
    } catch (err: unknown) {
      setCheckoutError(err instanceof Error ? err.message : 'حدث خطأ أثناء إتمام الطلب.');
    } finally {
      setIsProcessingPayment(false);
    }
  };

  // Apply Discount with Manager Auth Gate
  const handleApplyDiscount = () => {
    const discAmount = Number(discountInput) || 0;
    if (discAmount <= 0) {
      setDiscount(0, '');
      setDiscountModalOpen(false);
      return;
    }

    if (currentUser.role === 'cashier') {
      // Prompt manager approval
      requestManagerAuth(
        `تطبيق خصم بقيمة ${discAmount} ج.م`,
        `طلب الكاشير ${currentUser.name} تطبيق خصم على طلب بقيمة ${cartSubtotal} ج.م - السبب: ${discountReasonInput || 'بدون سبب'}`,
        true,
        (managerName, reason) => {
          setDiscount(discAmount, `${discountReasonInput || ''} (اعتماد: ${managerName})`);
          setDiscountModalOpen(false);
        }
      );
    } else {
      setDiscount(discAmount, discountReasonInput);
      setDiscountModalOpen(false);
    }
  };

  const grandTotal = cartTotal + (orderType === 'delivery' ? settings.deliveryFee : 0);
  const changeDue = Math.max(0, (Number(amountPaidInput) || 0) - grandTotal);

  if (!activeShift) {
    return (
      <div className="flex h-full items-center justify-center bg-[#F3F6FA] p-6" dir="rtl">
        <div className="w-full max-w-lg rounded-3xl border border-slate-200 bg-white p-8 text-center shadow-sm">
          <div className="mx-auto mb-4 flex h-16 w-16 items-center justify-center rounded-2xl bg-amber-50 text-amber-600">
            <Clock className="h-8 w-8" />
          </div>
          <h1 className="text-xl font-black text-slate-900">لا توجد وردية مفتوحة</h1>
          <p className="mt-2 text-sm leading-6 text-slate-500">
            يجب فتح وردية وتسجيل الرصيد الافتتاحي قبل استخدام نقطة البيع أو تسجيل أي عملية مالية.
          </p>
          <button
            type="button"
            onClick={() => setActiveModule('shifts')}
            className="mt-6 rounded-xl bg-emerald-600 px-6 py-3 text-sm font-black text-white shadow-md hover:bg-emerald-700"
          >
            فتح وردية الآن
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className="flex h-full overflow-hidden bg-[#F3F6FA] font-sans" dir="rtl">
      {/* 1. LEFT PANEL: CURRENT ORDER CART & CHECKOUT (Width: 380px) */}
      <div className="w-[390px] bg-[#151C24] border-l border-[#2C343E] flex flex-col h-full shadow-xl shrink-0 z-10">
        {/* Cart Header */}
        <div className="p-3 bg-[#151B23] text-white border-b border-[#2C343E]">
          <div className="mb-3 flex items-center justify-between">
            <div><h2 className="text-sm font-black">الطلب الحالي</h2><p className="text-[10px] text-slate-400">اختر نوع الطلب ثم أضف الأصناف</p></div>
            <span className="rounded-lg bg-[#0F52BA] px-2.5 py-1 text-xs font-black text-white">{cart.reduce((sum, item) => sum + item.quantity, 0)} صنف</span>
          </div>
          {/* Order Type Tabs */}
          <div className="grid grid-cols-3 gap-1.5 p-1 bg-[#232931] rounded-xl text-xs font-bold mb-2.5">
            <button
              type="button"
              onClick={() => setOrderType('dine_in')}
              className={`py-2 rounded-lg flex items-center justify-center gap-1.5 transition-all ${
                orderType === 'dine_in'
                  ? 'bg-red-600 text-white shadow-xs'
                  : 'text-slate-400 hover:text-white hover:bg-slate-700'
              }`}
            >
              <Utensils className="w-3.5 h-3.5" />
              <span>صالة</span>
            </button>
            <button
              type="button"
              onClick={() => setOrderType('takeaway')}
              className={`py-2 rounded-lg flex items-center justify-center gap-1.5 transition-all ${
                orderType === 'takeaway'
                  ? 'bg-red-600 text-white shadow-xs'
                  : 'text-slate-400 hover:text-white hover:bg-slate-700'
              }`}
            >
              <ShoppingBag className="w-3.5 h-3.5" />
              <span>تيك أواي</span>
            </button>
            <button
              type="button"
              onClick={() => setOrderType('delivery')}
              className={`py-2 rounded-lg flex items-center justify-center gap-1.5 transition-all ${
                orderType === 'delivery'
                  ? 'bg-red-600 text-white shadow-xs'
                  : 'text-slate-400 hover:text-white hover:bg-slate-700'
              }`}
            >
              <Truck className="w-3.5 h-3.5" />
              <span>دليفري</span>
            </button>
          </div>

          {/* Contextual Order Inputs */}
          {orderType === 'dine_in' && (
            <div className="flex items-center gap-2">
              <span className="text-xs text-slate-400 shrink-0">رقم الطاولة:</span>
              <input
                ref={searchInputRef}
                type="text"
                value={tableNumber}
                onChange={(e) => setTableNumber(e.target.value)}
                placeholder="مثال: صالة 1 / طاولة 4"
                className="w-full bg-slate-800 text-white text-xs px-2.5 py-1.5 rounded-lg border border-slate-700 focus:outline-hidden focus:border-red-500"
              />
            </div>
          )}

          {orderType === 'delivery' && (
            <div className="space-y-1.5 text-xs">
              <div className="flex gap-1.5">
                <input
                  type="text"
                  value={customerName}
                  onChange={(e) => setCustomerName(e.target.value)}
                  placeholder="اسم العميل"
                  className="w-1/2 bg-slate-800 text-white px-2 py-1 rounded-lg border border-slate-700"
                />
                <input
                  type="text"
                  value={customerPhone}
                  onChange={(e) => setCustomerPhone(e.target.value)}
                  onBlur={() => { const previous = orders.find(order => order.orderType === 'delivery' && order.customerPhone === customerPhone); if (previous) { if (!customerName) setCustomerName(previous.customerName || ''); if (!deliveryAddress) setDeliveryAddress(previous.deliveryAddress || ''); } }}
                  placeholder="رقم الهاتف"
                  className="w-1/2 bg-slate-800 text-white px-2 py-1 rounded-lg border border-slate-700"
                />
              </div>
              <input
                type="text"
                value={deliveryAddress}
                onChange={(e) => setDeliveryAddress(e.target.value)}
                placeholder="عنوان التوصيل (العمارة، الشقة، المنطقة)"
                className="w-full bg-slate-800 text-white px-2 py-1 rounded-lg border border-slate-700"
              />
            </div>
          )}
        </div>

        {/* Cart Items List */}
        <div className="flex-1 overflow-y-auto p-3 space-y-2 bg-[#151C24]">
          {cart.length === 0 ? (
            <div className="h-full flex flex-col items-center justify-center text-slate-500 p-6 text-center">
              <div className="w-14 h-14 rounded-2xl bg-[#202A35] flex items-center justify-center text-slate-500 mb-3">
                <ShoppingBag className="w-7 h-7" />
              </div>
              <p className="font-bold text-sm text-slate-200">السلة فارغة</p>
              <p className="text-xs text-slate-400 mt-1">اضغط على الأصناف من القائمة لإضافتها للطلب</p>
            </div>
          ) : (
            cart.map((item) => (
              <div
                key={item.id}
                className="bg-[#1B242E] p-3 rounded-xl border border-[#2C3744] hover:border-blue-500 transition-all text-xs text-white"
              >
                <div className="flex justify-between items-start">
                  <div className="space-y-0.5">
                    <div className="font-bold text-white text-[13px] flex items-center gap-1.5">
                      <span>{item.productName}</span>
                      {item.sendToGrill && (
                        <span className="text-[10px] bg-amber-100 text-amber-800 px-1.5 py-0.2 rounded-sm font-semibold flex items-center gap-0.5">
                          <Flame className="w-2.5 h-2.5" /> فحم
                        </span>
                      )}
                    </div>
                    {item.variantName && (
                      <span className="inline-block text-[11px] font-bold text-red-600 bg-red-50 px-1.5 py-0.5 rounded-md">
                        {item.variantName}
                      </span>
                    )}
                    {item.notes && (
                      <p className="text-[11px] text-amber-800 bg-amber-50 px-1.5 py-0.5 rounded-sm">
                        ملاحظة: {item.notes}
                      </p>
                    )}
                  </div>
                  <button
                    type="button"
                    onClick={() => removeCartItem(item.id)}
                    className="text-slate-400 hover:text-red-500 p-1 transition-colors"
                  >
                    <Trash2 className="w-3.5 h-3.5" />
                  </button>
                </div>

                {/* Quantity and Price Bar */}
                <div className="flex justify-between items-center pt-2 mt-1 border-t border-[#2C3744]">
                  <div className="flex items-center gap-1.5">
                    <button
                      type="button"
                      onClick={() => updateCartItemQty(item.id, -1)}
                      className="w-7 h-7 rounded-lg bg-slate-100 hover:bg-slate-200 text-slate-700 flex items-center justify-center font-bold transition-colors"
                    >
                      <Minus className="w-3 h-3" />
                    </button>
                    <span className="w-7 text-center font-black text-sm text-white">{item.quantity}</span>
                    <button
                      type="button"
                      onClick={() => updateCartItemQty(item.id, 1)}
                      className="w-7 h-7 rounded-lg bg-red-100 hover:bg-red-200 text-red-700 flex items-center justify-center font-bold transition-colors"
                    >
                      <Plus className="w-3 h-3" />
                    </button>
                  </div>
                  <div className="text-left">
                    <span className="font-extrabold text-sm text-white">{item.totalPrice} ج</span>
                    <span className="text-[10px] text-slate-400 block font-normal">({item.unitPrice} ج/قطعة)</span>
                  </div>
                </div>
              </div>
            ))
          )}
        </div>

        {/* Cart Financial Summary & Checkout Button */}
        <div className="p-4 bg-[#11171E] border-t border-[#2C343E] space-y-3 shrink-0 text-slate-300">
          <div className="space-y-1 text-xs">
            <div className="flex justify-between text-slate-600">
              <span>المجموع الفرعي:</span>
              <span className="font-bold">{cartSubtotal} ج</span>
            </div>

            {discount > 0 && (
              <div className="flex justify-between text-red-600 font-semibold">
                <span>الخصم المطبق:</span>
                <span>- {discount} ج</span>
              </div>
            )}

            {orderType === 'delivery' && (
              <div className="flex justify-between text-slate-600">
                <span>خدمة التوصيل:</span>
                <span className="font-bold">+ {settings.deliveryFee} ج</span>
              </div>
            )}

            <div className="flex justify-between items-center pt-1.5 border-t border-slate-200">
              <span className="font-extrabold text-sm text-white">الإجمالي النهائي:</span>
              <span className="text-2xl font-black text-white">{grandTotal} ج.م</span>
            </div>
          </div>

          {/* Quick Actions (Discount & Clear) */}
          <div className="flex gap-2">
            <button
              type="button"
              onClick={() => {
                setDiscountInput(discount > 0 ? String(discount) : '');
                setDiscountModalOpen(true);
              }}
              className="flex-1 py-1.5 px-2 bg-slate-100 hover:bg-slate-200 text-slate-700 text-xs font-bold rounded-xl transition-colors flex items-center justify-center gap-1.5"
            >
              <Percent className="w-3.5 h-3.5 text-amber-600" />
              <span>{discount > 0 ? `خصم (${discount}ج)` : 'إضافة خصم'}</span>
            </button>
            <button
              type="button"
              onClick={clearCart}
              disabled={cart.length === 0}
              className="py-1.5 px-3 bg-slate-100 hover:bg-red-50 text-slate-600 hover:text-red-600 disabled:opacity-40 disabled:pointer-events-none text-xs font-bold rounded-xl transition-colors flex items-center justify-center gap-1"
              title="إفراغ السلة"
            >
              <RotateCcw className="w-3.5 h-3.5" />
              <span>إلغاء</span>
            </button>
          </div>

          {/* Big Touch Checkout Button */}
          <button
            type="button"
            onClick={handleOpenCheckout}
            disabled={cart.length === 0}
            className="w-full py-3.5 bg-[#0866E8] hover:bg-blue-600 active:bg-blue-700 disabled:bg-slate-700 disabled:text-slate-500 disabled:shadow-none disabled:pointer-events-none text-white text-base font-black rounded-xl shadow-md shadow-blue-950/30 transition-all flex items-center justify-center gap-2"
          >
            <Banknote className="w-5 h-5" />
            <span>دفع وإنهاء الطلب ({grandTotal} ج)</span>
          </button>
        </div>
      </div>

      {/* 2. CENTER & RIGHT: PRODUCT CATALOG & CATEGORIES */}
      <div className="flex-1 flex flex-col h-full overflow-hidden">
        {/* Top Filter & Search Bar */}
        <div className="bg-white border-b border-slate-200 px-4 py-3 flex items-center justify-between gap-4 shrink-0">
          {/* Search Input */}
          <div className="relative flex-1 max-w-xl">
            <Search className="w-4 h-4 text-slate-400 absolute right-3 top-1/2 -translate-y-1/2" />
            <input
              type="text"
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              placeholder="ابحث عن صنف بالاسم (كفتة، شاورما، بطاطس، مكرونة...)"
              className="w-full bg-[#F4F7FB] text-slate-800 text-sm pr-10 pl-4 py-2.5 rounded-xl border border-slate-200 focus:outline-hidden focus:border-[#0F52BA] focus:bg-white focus:ring-4 focus:ring-blue-50"
            />
            {searchQuery && (
              <button
                type="button"
                onClick={() => setSearchQuery('')}
                className="absolute left-3 top-1/2 -translate-y-1/2 text-slate-400 hover:text-slate-600"
              >
                <X className="w-3.5 h-3.5" />
              </button>
            )}
          </div>

          {/* Cashier Status Info */}
          <div className="flex items-center gap-3 text-xs text-slate-600">
            <div className="flex items-center gap-1.5 bg-slate-100 px-3 py-1.5 rounded-xl font-medium">
              <span className="text-slate-400">الكاشير:</span>
              <span className="font-bold text-slate-800">{currentUser.name}</span>
            </div>
            <div className="flex items-center gap-1.5 bg-blue-50 text-[#0F52BA] px-3 py-1.5 rounded-xl font-bold border border-blue-100">
              <span>الأصناف المعروضة:</span>
              <span>{filteredProducts.length} صنف</span>
            </div>
          </div>
        </div>

        {/* Categories Strip */}
        <div className="bg-white border-b border-slate-200 px-4 py-2.5 flex items-center gap-2 overflow-x-auto shrink-0 shadow-sm">
          {categories.map((cat) => {
            const isSelected = !searchQuery && selectedCategory === cat.id;
            return (
              <button
                key={cat.id}
                type="button"
                onClick={() => {
                  setSelectedCategory(cat.id);
                  setSearchQuery('');
                }}
                className={`min-w-28 h-20 px-4 py-2.5 rounded-xl text-xs font-bold whitespace-nowrap transition-all duration-150 flex flex-col items-center justify-center gap-2 shrink-0 border ${
                  isSelected
                    ? 'bg-[#0866E8] text-white shadow-md shadow-blue-200 border-blue-600'
                    : 'bg-white hover:bg-slate-50 text-slate-700 border-slate-200'
                }`}
              >
                <span className="text-2xl">{cat.icon || '🍽️'}</span>
                <span>{cat.name}</span>
              </button>
            );
          })}
        </div>

        {/* Products Grid (Large Touch Targets) */}
        <div className="flex-1 overflow-y-auto p-4">
          <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5 xl:grid-cols-6 gap-3.5">
            {filteredProducts.map((product) => {
              const hasVariants = product.variants && product.variants.length > 0;
              return (
                <button
                  key={product.id}
                  type="button"
                  onClick={() => handleProductClick(product)}
                  className="bg-white rounded-xl p-2.5 border border-slate-200 shadow-sm shadow-slate-200/50 hover:shadow-lg hover:shadow-blue-100/70 hover:border-blue-400 active:scale-[0.98] transition-all text-right flex flex-col justify-between h-52 relative group focus-visible:ring-4 focus-visible:ring-blue-100"
                >
                  {/* Tags */}
                  <div className="flex items-center justify-between w-full">
                    {product.sendToGrill ? (
                      <span className="text-[10px] font-bold bg-amber-50 text-amber-700 border border-amber-200 px-1.5 py-0.2 rounded-md flex items-center gap-0.5">
                        <Flame className="w-2.5 h-2.5 text-amber-600" /> فحم
                      </span>
                    ) : (
                      <span></span>
                    )}

                    {hasVariants && (
                      <span className="text-[10px] font-bold bg-slate-100 text-slate-600 px-1.5 py-0.2 rounded-md flex items-center gap-0.5">
                        <Layers className="w-2.5 h-2.5" /> خيارات
                      </span>
                    )}
                  </div>

                  <div className="relative h-20 w-full overflow-hidden rounded-lg bg-white"><Image src="/products/kofta-sandwich.png" alt={product.name} fill sizes="180px" className="object-contain transition-transform duration-200 group-hover:scale-105" /></div>

                  {/* Product Name */}
                  <div className="font-extrabold text-slate-900 text-[15px] leading-snug line-clamp-2 mt-1">
                    {product.name}
                  </div>

                  {hasVariants && (
                    <div className="grid w-full grid-cols-3 gap-1">
                      {product.variants.slice(0, 3).map((variant, index) => (
                        <span key={variant.id} className={`truncate rounded-md border px-1 py-1 text-center text-[9px] font-bold ${index === 0 ? 'border-blue-600 bg-blue-600 text-white' : 'border-slate-200 bg-slate-50 text-slate-500'}`}>{variant.name}</span>
                      ))}
                    </div>
                  )}

                  {/* Price Bar */}
                  <div className="flex justify-between items-center w-full pt-2 border-t border-slate-100">
                    <span className="flex h-7 w-7 items-center justify-center rounded-lg bg-blue-50 text-[#0F52BA] transition-colors group-hover:bg-[#0F52BA] group-hover:text-white"><Plus className="h-4 w-4" /></span>
                    <span className="text-[10px] text-slate-400">
                      {hasVariants ? 'يبدأ من' : 'السعر'}
                    </span>
                    <span className="font-black text-base text-[#0F52BA]">
                      {product.basePrice} ج
                    </span>
                  </div>
                </button>
              );
            })}
          </div>
        </div>
      </div>

      {/* 3. PRODUCT VARIANT SELECTION POPUP MODAL (Fast Touch Overlay) */}
      {variantModalProduct && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 backdrop-blur-xs p-4 animate-in fade-in">
          <div className="bg-white rounded-3xl shadow-2xl max-w-md w-full overflow-hidden border border-slate-200 p-6 space-y-5 text-slate-800">
            <div className="flex items-start justify-between">
              <div>
                <span className="text-xs font-bold text-red-600 uppercase tracking-wide">اختيار نوع الخبز / الحجم</span>
                <h3 className="text-xl font-black text-slate-900 mt-0.5">{variantModalProduct.name}</h3>
              </div>
              <button
                type="button"
                onClick={() => setVariantModalProduct(null)}
                className="p-1.5 text-slate-400 hover:text-slate-600 rounded-lg"
              >
                <X className="w-5 h-5" />
              </button>
            </div>

            {/* Variant buttons */}
            <div className="space-y-2">
              <label className="text-xs font-bold text-slate-700 block">حدد الخيار المطلوب:</label>
              <div className="grid grid-cols-3 gap-2">
                {variantModalProduct.variants.map((variant) => {
                  const isSelected = selectedVariant?.id === variant.id;
                  return (
                    <button
                      key={variant.id}
                      type="button"
                      onClick={() => setSelectedVariant(variant)}
                      className={`p-3 rounded-2xl border-2 text-center transition-all ${
                        isSelected
                          ? 'border-red-600 bg-red-50 text-red-900 shadow-sm'
                          : 'border-slate-200 hover:border-slate-300 text-slate-700'
                      }`}
                    >
                      <div className="font-black text-sm">{variant.name}</div>
                      <div className="font-extrabold text-xs text-red-600 mt-1">{variant.price} ج</div>
                    </button>
                  );
                })}
              </div>
            </div>

            {/* Notes Input */}
            <div>
              <label className="text-xs font-bold text-slate-700 block mb-1">ملاحظة التحضير (اختياري):</label>
              <input
                type="text"
                value={itemNotes}
                onChange={(e) => setItemNotes(e.target.value)}
                placeholder="مثال: بدون طماطم، طحينة زيادة، خبز محمص..."
                className="w-full text-xs bg-slate-50 border border-slate-200 rounded-xl px-3 py-2 text-slate-800 focus:outline-hidden focus:ring-2 focus:ring-red-500"
              />
            </div>

            {/* Actions */}
            <div className="flex gap-3 pt-2">
              <button
                type="button"
                onClick={() => setVariantModalProduct(null)}
                className="flex-1 py-3 bg-slate-100 hover:bg-slate-200 text-slate-700 text-sm font-bold rounded-xl"
              >
                إلغاء
              </button>
              <button
                type="button"
                onClick={handleConfirmVariant}
                disabled={!selectedVariant}
                className="flex-1 py-3 bg-red-600 hover:bg-red-700 active:bg-red-800 disabled:opacity-40 text-white text-sm font-black rounded-xl shadow-md flex items-center justify-center gap-1.5"
              >
                <Check className="w-4 h-4" />
                إضافة للطلب ({selectedVariant?.price || 0} ج)
              </button>
            </div>
          </div>
        </div>
      )}

      {/* 4. FAST CHECKOUT MODAL WITH TOUCH NUMERIC KEYPAD */}
      {checkoutModalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/70 backdrop-blur-xs p-4 animate-in fade-in">
          <div className="bg-white rounded-3xl shadow-2xl max-w-xl w-full overflow-hidden border border-slate-200 flex flex-col text-slate-800">
            {/* Header */}
            <div className="bg-slate-900 text-white px-6 py-4 flex items-center justify-between">
              <div>
                <h3 className="font-extrabold text-lg">إنهاء ودفع الطلب</h3>
                <p className="text-xs text-slate-400">
                  {orderType === 'dine_in' ? 'طلب صالة' : orderType === 'takeaway' ? 'تيك أواي' : 'دليفري'} - عدد
                  الأصناف: {cart.reduce((s, i) => s + i.quantity, 0)}
                </p>
              </div>
              <button
                type="button"
                onClick={() => setCheckoutModalOpen(false)}
                className="p-1.5 text-slate-400 hover:text-white rounded-lg"
              >
                <X className="w-5 h-5" />
              </button>
            </div>

            {/* Modal Body */}
            <div className="p-6 space-y-4">
              {/* Payment Method Selector */}
              <div className="grid grid-cols-3 gap-3">
                <button
                  type="button"
                  onClick={() => {
                    setPaymentMethod('cash');
                    setAmountPaidInput(String(grandTotal));
                  }}
                  className={`py-3 px-4 rounded-2xl border-2 font-bold text-sm flex items-center justify-center gap-2 transition-all ${
                    paymentMethod === 'cash'
                      ? 'border-emerald-600 bg-emerald-50 text-emerald-900 shadow-sm'
                      : 'border-slate-200 text-slate-700 hover:bg-slate-50'
                  }`}
                >
                  <Banknote className="w-5 h-5 text-emerald-600" />
                  <span>دفع نقدي (كاش)</span>
                </button>
                <button
                  type="button"
                  onClick={() => {
                    setPaymentMethod('card');
                    setAmountPaidInput(String(grandTotal));
                  }}
                  className={`py-3 px-4 rounded-2xl border-2 font-bold text-sm flex items-center justify-center gap-2 transition-all ${
                    paymentMethod === 'card'
                      ? 'border-blue-600 bg-blue-50 text-blue-900 shadow-sm'
                      : 'border-slate-200 text-slate-700 hover:bg-slate-50'
                  }`}
                >
                  <CreditCard className="w-5 h-5 text-blue-600" />
                  <span>بطاقة بنكية (فيزا)</span>
                </button>
                <button
                  type="button"
                  onClick={() => {
                    setPaymentMethod('instapay');
                    setAmountPaidInput(String(grandTotal));
                  }}
                  className={`py-3 px-4 rounded-2xl border-2 font-bold text-sm flex items-center justify-center gap-2 transition-all ${
                    paymentMethod === 'instapay'
                      ? 'border-violet-600 bg-violet-50 text-violet-900 shadow-sm'
                      : 'border-slate-200 text-slate-700 hover:bg-slate-50'
                  }`}
                >
                  <Smartphone className="w-5 h-5 text-violet-600" />
                  <span>InstaPay</span>
                </button>
              </div>

              {/* Total & Paid Display */}
              <div className="bg-slate-50 border border-slate-200 rounded-2xl p-4 space-y-2">
                <div className="flex justify-between items-center">
                  <span className="text-sm font-bold text-slate-600">إجمالي المبلغ المطلوب:</span>
                  <span className="text-2xl font-black text-slate-900">{grandTotal} ج.م</span>
                </div>

                {paymentMethod === 'cash' && (
                  <>
                    <div className="flex justify-between items-center pt-2 border-t border-slate-200">
                      <span className="text-sm font-bold text-slate-700">المبلغ المدفوع:</span>
                      <span className="text-2xl font-black text-emerald-700 font-mono">
                        {amountPaidInput || '0'} ج.م
                      </span>
                    </div>

                    <div className="flex justify-between items-center pt-1 text-sm font-extrabold">
                      <span className="text-slate-600">المتبقي (الباقي للعميل):</span>
                      <span className={`text-lg font-black ${changeDue > 0 ? 'text-blue-600' : 'text-slate-400'}`}>
                        {changeDue} ج.م
                      </span>
                    </div>
                  </>
                )}
              </div>

              {checkoutError && (
                <div className="bg-red-50 text-red-700 text-xs font-bold p-2.5 rounded-xl border border-red-200 text-center">
                  {checkoutError}
                </div>
              )}

              {/* Quick Cash & Keypad for Cash Payment */}
              {paymentMethod === 'cash' && (
                <div className="space-y-3">
                  {/* Quick Cash Buttons */}
                  <div className="flex gap-2">
                    {[grandTotal, 50, 100, 200, 500].map((amt, idx) => (
                      <button
                        key={idx}
                        type="button"
                        onClick={() => handleQuickCash(amt)}
                        className="flex-1 py-2 bg-slate-100 hover:bg-slate-200 font-bold text-xs rounded-xl text-slate-700 transition-colors border border-slate-200"
                      >
                        {idx === 0 ? 'المبلغ بالضبط' : `${amt} ج`}
                      </button>
                    ))}
                  </div>

                  {/* Touch Numeric Keypad */}
                  <div className="grid grid-cols-3 gap-2 max-w-sm mx-auto">
                    {['1', '2', '3', '4', '5', '6', '7', '8', '9'].map((num) => (
                      <button
                        key={num}
                        type="button"
                        onClick={() => handleKeypadPress(num)}
                        className="h-11 font-bold text-lg bg-slate-100 hover:bg-slate-200 rounded-xl text-slate-800 transition-colors border border-slate-200"
                      >
                        {num}
                      </button>
                    ))}
                    <button
                      type="button"
                      onClick={() => handleKeypadPress('clear')}
                      className="h-11 font-bold text-xs bg-slate-100 hover:bg-slate-200 rounded-xl text-slate-600"
                    >
                      مسح
                    </button>
                    <button
                      type="button"
                      onClick={() => handleKeypadPress('0')}
                      className="h-11 font-bold text-lg bg-slate-100 hover:bg-slate-200 rounded-xl text-slate-800"
                    >
                      0
                    </button>
                    <button
                      type="button"
                      onClick={() => handleKeypadPress('back')}
                      className="h-11 font-bold text-sm bg-slate-100 hover:bg-slate-200 rounded-xl text-slate-600"
                    >
                      ⌫
                    </button>
                  </div>
                </div>
              )}

              {/* Action Buttons */}
              <div className="flex gap-3 pt-2">
                <button
                  type="button"
                  onClick={() => setCheckoutModalOpen(false)}
                  className="flex-1 py-3.5 bg-slate-100 hover:bg-slate-200 text-slate-700 text-sm font-bold rounded-2xl"
                >
                  رجوع
                </button>
                <button
                  type="button"
                  onClick={handleFinalCheckout}
                  disabled={isProcessingPayment}
                  className="flex-2 py-3.5 bg-emerald-600 hover:bg-emerald-700 active:bg-emerald-800 text-white text-base font-black rounded-2xl shadow-lg transition-all flex items-center justify-center gap-2"
                >
                  <Check className="w-5 h-5" />
                   <span>{isProcessingPayment ? 'جارٍ تسجيل العملية...' : 'تأكيد الدفع وطباعة الإيصالات'}</span>
                </button>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* 5. DISCOUNT MODAL */}
      {discountModalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 backdrop-blur-xs p-4 animate-in fade-in">
          <div className="bg-white rounded-3xl shadow-2xl max-w-md w-full overflow-hidden border border-slate-200 p-6 space-y-4 text-slate-800">
            <div className="flex justify-between items-center">
              <h3 className="font-extrabold text-lg text-slate-900">تطبيق خصم على الطلب</h3>
              <button
                type="button"
                onClick={() => setDiscountModalOpen(false)}
                className="p-1 text-slate-400 hover:text-slate-600"
              >
                <X className="w-5 h-5" />
              </button>
            </div>

            <div>
              <label className="text-xs font-bold text-slate-700 block mb-1">قيمة الخصم (بالجنيه):</label>
              <input
                type="number"
                value={discountInput}
                onChange={(e) => setDiscountInput(e.target.value)}
                placeholder="أدخل مبلغ الخصم"
                className="w-full text-base font-bold bg-slate-50 border border-slate-200 rounded-xl px-3 py-2 text-slate-900"
              />
            </div>

            <div>
              <label className="text-xs font-bold text-slate-700 block mb-1">سبب الخصم:</label>
              <input
                type="text"
                value={discountReasonInput}
                onChange={(e) => setDiscountReasonInput(e.target.value)}
                placeholder="مثال: خصم ضيافة، عميل دائم، إلخ..."
                className="w-full text-xs bg-slate-50 border border-slate-200 rounded-xl px-3 py-2 text-slate-800"
              />
            </div>

            {currentUser.role === 'cashier' && (
              <p className="text-[11px] text-amber-700 bg-amber-50 p-2 rounded-lg border border-amber-200">
                ⚠️ سيطلب النظام موافقة المدير وتوثيق رمز PIN عند تطبيق الخصم.
              </p>
            )}

            <div className="flex gap-2 pt-2">
              <button
                type="button"
                onClick={() => setDiscountModalOpen(false)}
                className="flex-1 py-2.5 bg-slate-100 hover:bg-slate-200 text-slate-700 text-xs font-bold rounded-xl"
              >
                إلغاء
              </button>
              <button
                type="button"
                onClick={handleApplyDiscount}
                className="flex-1 py-2.5 bg-red-600 hover:bg-red-700 text-white text-xs font-bold rounded-xl"
              >
                تطبيق الخصم
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
