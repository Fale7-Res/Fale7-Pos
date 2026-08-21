'use client';

import React, { useState } from 'react';
import { useAppStore } from '@/lib/store';
import { Product, ProductVariant, Category } from '@/lib/types';
import { UtensilsCrossed, Plus, Edit2, Trash2, Flame, Layers, Search, X, Check } from 'lucide-react';

export default function MenuManagementScreen() {
  const { categories, products, addProduct, updateProduct, deleteProduct } = useAppStore();
  const [searchQuery, setSearchQuery] = useState('');
  const [selectedCat, setSelectedCat] = useState('all');

  // Product modal state
  const [modalOpen, setModalOpen] = useState(false);
  const [editProduct, setEditProduct] = useState<Product | null>(null);
  const [formData, setFormData] = useState<{
    name: string;
    categoryId: string;
    basePrice: number;
    sendToGrill: boolean;
    isAvailable: boolean;
    variants: ProductVariant[];
  }>({
    name: '',
    categoryId: categories[0]?.id || '',
    basePrice: 50,
    sendToGrill: false,
    isAvailable: true,
    variants: [],
  });

  // Variant input inside modal
  const [varName, setVarName] = useState('');
  const [varPrice, setVarPrice] = useState('');

  const filteredProducts = products.filter((p) => {
    const matchesSearch = p.name.toLowerCase().includes(searchQuery.toLowerCase());
    const matchesCat = selectedCat === 'all' || p.categoryId === selectedCat;
    return matchesSearch && matchesCat;
  });

  const handleOpenModal = (prod?: Product) => {
    if (prod) {
      setEditProduct(prod);
      setFormData({
        name: prod.name,
        categoryId: prod.categoryId,
        basePrice: prod.basePrice,
        sendToGrill: prod.sendToGrill,
        isAvailable: prod.isAvailable,
        variants: [...prod.variants],
      });
    } else {
      setEditProduct(null);
      setFormData({
        name: '',
        categoryId: categories[0]?.id || '',
        basePrice: 50,
        sendToGrill: false,
        isAvailable: true,
        variants: [],
      });
    }
    setVarName('');
    setVarPrice('');
    setModalOpen(true);
  };

  const handleAddVariant = () => {
    if (!varName.trim() || !varPrice) return;
    const newVar: ProductVariant = {
      id: `var-${Date.now()}`,
      name: varName.trim(),
      price: Number(varPrice),
    };
    setFormData((prev) => ({
      ...prev,
      variants: [...prev.variants, newVar],
    }));
    setVarName('');
    setVarPrice('');
  };

  const handleRemoveVariant = (id: string) => {
    setFormData((prev) => ({
      ...prev,
      variants: prev.variants.filter((v) => v.id !== id),
    }));
  };

  const handleSaveProduct = (e: React.FormEvent) => {
    e.preventDefault();
    if (!formData.name.trim()) return;

    if (editProduct) {
      updateProduct({
        ...editProduct,
        name: formData.name,
        categoryId: formData.categoryId,
        basePrice: formData.basePrice,
        sendToGrill: formData.sendToGrill,
        isAvailable: formData.isAvailable,
        variants: formData.variants,
      });
    } else {
      addProduct({
        name: formData.name,
        categoryId: formData.categoryId,
        basePrice: formData.basePrice,
        sendToGrill: formData.sendToGrill,
        isAvailable: formData.isAvailable,
        variants: formData.variants,
        sortOrder: products.length + 1,
      });
    }
    setModalOpen(false);
  };

  return (
    <div className="h-full overflow-y-auto bg-slate-100 p-6 space-y-6 font-sans select-none" dir="rtl">
      {/* 1. Header */}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 bg-white p-5 rounded-3xl border border-slate-200 shadow-xs">
        <div className="flex items-center gap-3">
          <div className="w-10 h-10 rounded-2xl bg-red-500/10 text-red-600 border border-red-500/20 flex items-center justify-center">
            <UtensilsCrossed className="w-6 h-6" />
          </div>
          <div>
            <h1 className="text-xl font-black text-slate-900">إدارة قائمة الطعام والأسعار (المنيو)</h1>
            <p className="text-xs text-slate-500">
              إضافة وتعديل الأصناف، الأسعار، أنواع الخبز والأحجام، وتحديد الأصناف التي ترسل لشاشة الفحم
            </p>
          </div>
        </div>

        <div className="flex items-center gap-3">
          <div className="relative">
            <Search className="w-4 h-4 text-slate-400 absolute right-3 top-1/2 -translate-y-1/2" />
            <input
              type="text"
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              placeholder="ابحث في الأصناف..."
              className="bg-slate-100 text-xs pr-9 pl-3 py-2 rounded-xl border border-slate-200 focus:outline-hidden"
            />
          </div>

          <select
            value={selectedCat}
            onChange={(e) => setSelectedCat(e.target.value)}
            className="bg-slate-100 text-xs px-3 py-2 rounded-xl border border-slate-200 font-bold text-slate-700"
          >
            <option value="all">كل الأقسام</option>
            {categories.map((c) => (
              <option key={c.id} value={c.id}>
                {c.name}
              </option>
            ))}
          </select>

          <button
            type="button"
            onClick={() => handleOpenModal()}
            className="px-4 py-2.5 bg-red-600 hover:bg-red-700 text-white text-xs font-bold rounded-xl shadow-md transition-all flex items-center gap-1.5"
          >
            <Plus className="w-4 h-4" />
            <span>إضافة صنف جديد</span>
          </button>
        </div>
      </div>

      {/* 2. Products Table */}
      <div className="bg-white rounded-3xl border border-slate-200 shadow-xs overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full text-right text-xs">
            <thead>
              <tr className="bg-slate-100/70 border-b border-slate-200 text-slate-600 font-bold">
                <th className="py-3.5 px-4">الصنف</th>
                <th className="py-3.5 px-4">القسم</th>
                <th className="py-3.5 px-4 text-center">السعر الأساسي</th>
                <th className="py-3.5 px-4 text-center">شاشة الفحم</th>
                <th className="py-3.5 px-4">خيارات الخبز / الحجم</th>
                <th className="py-3.5 px-4 text-center">الحالة</th>
                <th className="py-3.5 px-4 text-center">الإجراءات</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {filteredProducts.map((p) => {
                const cat = categories.find((c) => c.id === p.categoryId);
                return (
                  <tr key={p.id} className="hover:bg-slate-50 transition-colors">
                    <td className="py-3.5 px-4 font-black text-slate-900 text-sm">{p.name}</td>
                    <td className="py-3.5 px-4 font-semibold text-slate-700">{cat?.name || '-'}</td>
                    <td className="py-3.5 px-4 text-center font-bold text-red-600 text-sm">{p.basePrice} ج</td>
                    <td className="py-3.5 px-4 text-center">
                      {p.sendToGrill ? (
                        <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-[10px] font-bold bg-amber-100 text-amber-800">
                          <Flame className="w-3 h-3 text-amber-600" />
                          <span>نعم (فحم)</span>
                        </span>
                      ) : (
                        <span className="text-slate-400">لا</span>
                      )}
                    </td>
                    <td className="py-3.5 px-4">
                      {p.variants && p.variants.length > 0 ? (
                        <div className="flex gap-1 flex-wrap">
                          {p.variants.map((v) => (
                            <span
                              key={v.id}
                              className="px-2 py-0.5 bg-slate-100 border border-slate-200 rounded-md text-[10px] text-slate-700 font-medium"
                            >
                              {v.name}: {v.price}ج
                            </span>
                          ))}
                        </div>
                      ) : (
                        <span className="text-slate-400">سعر موحد</span>
                      )}
                    </td>
                    <td className="py-3.5 px-4 text-center">
                      <span
                        className={`inline-block px-2.5 py-0.5 rounded-full text-[10px] font-bold ${
                          p.isAvailable ? 'bg-emerald-100 text-emerald-800' : 'bg-slate-100 text-slate-500'
                        }`}
                      >
                        {p.isAvailable ? 'متاح للبيع' : 'غير متوفر'}
                      </span>
                    </td>
                    <td className="py-3.5 px-4 text-center">
                      <div className="flex items-center justify-center gap-1">
                        <button
                          type="button"
                          onClick={() => handleOpenModal(p)}
                          className="p-1.5 bg-slate-100 hover:bg-slate-200 text-slate-700 rounded-lg transition-colors"
                          title="تعديل الصنف"
                        >
                          <Edit2 className="w-3.5 h-3.5" />
                        </button>
                        <button
                          type="button"
                          onClick={() => deleteProduct(p.id)}
                          className="p-1.5 bg-red-50 hover:bg-red-100 text-red-600 rounded-lg transition-colors"
                          title="حذف الصنف"
                        >
                          <Trash2 className="w-3.5 h-3.5" />
                        </button>
                      </div>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      </div>

      {/* 3. ADD / EDIT PRODUCT MODAL */}
      {modalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 backdrop-blur-xs p-4 animate-in fade-in">
          <form
            onSubmit={handleSaveProduct}
            className="bg-white rounded-3xl shadow-2xl max-w-lg w-full overflow-hidden border border-slate-200 p-6 space-y-4 text-slate-800"
          >
            <div className="flex justify-between items-center border-b border-slate-100 pb-3">
              <h3 className="font-extrabold text-base text-slate-900">
                {editProduct ? `تعديل صنف: ${editProduct.name}` : 'إضافة صنف جديد للمنيو'}
              </h3>
              <button
                type="button"
                onClick={() => setModalOpen(false)}
                className="p-1 text-slate-400 hover:text-slate-600"
              >
                <X className="w-5 h-5" />
              </button>
            </div>

            <div>
              <label className="block text-xs font-bold text-slate-700 mb-1">اسم الصنف:</label>
              <input
                type="text"
                value={formData.name}
                onChange={(e) => setFormData({ ...formData, name: e.target.value })}
                placeholder="مثال: ساندوتش كفتة ع الفحم"
                className="w-full text-xs font-bold bg-slate-50 border border-slate-200 rounded-xl px-3 py-2 text-slate-900"
                required
                autoFocus
              />
            </div>

            <div className="grid grid-cols-2 gap-3">
              <div>
                <label className="block text-xs font-bold text-slate-700 mb-1">القسم:</label>
                <select
                  value={formData.categoryId}
                  onChange={(e) => setFormData({ ...formData, categoryId: e.target.value })}
                  className="w-full text-xs font-bold bg-slate-50 border border-slate-200 rounded-xl p-2 text-slate-900"
                >
                  {categories.map((c) => (
                    <option key={c.id} value={c.id}>
                      {c.name}
                    </option>
                  ))}
                </select>
              </div>

              <div>
                <label className="block text-xs font-bold text-slate-700 mb-1">السعر الأساسي (ج):</label>
                <input
                  type="number"
                  value={formData.basePrice}
                  onChange={(e) => setFormData({ ...formData, basePrice: Number(e.target.value) })}
                  className="w-full text-xs font-bold bg-slate-50 border border-slate-200 rounded-xl px-3 py-2 text-slate-900"
                  required
                />
              </div>
            </div>

            {/* Checkbox toggles */}
            <div className="flex items-center justify-between p-3 bg-slate-50 rounded-2xl border border-slate-200">
              <div className="flex items-center gap-2">
                <input
                  type="checkbox"
                  id="grillCheck"
                  checked={formData.sendToGrill}
                  onChange={(e) => setFormData({ ...formData, sendToGrill: e.target.checked })}
                  className="w-4 h-4 rounded-md accent-amber-600"
                />
                <label htmlFor="grillCheck" className="text-xs font-bold text-slate-800 cursor-pointer">
                  إرسال هذا الصنف لشاشة الفحم والمشويات
                </label>
              </div>

              <div className="flex items-center gap-2">
                <input
                  type="checkbox"
                  id="availCheck"
                  checked={formData.isAvailable}
                  onChange={(e) => setFormData({ ...formData, isAvailable: e.target.checked })}
                  className="w-4 h-4 rounded-md accent-emerald-600"
                />
                <label htmlFor="availCheck" className="text-xs font-bold text-slate-800 cursor-pointer">
                  متاح للبيع
                </label>
              </div>
            </div>

            {/* Variants Manager (Bread / Sizes) */}
            <div className="space-y-2 pt-1 border-t border-slate-100">
              <label className="block text-xs font-bold text-slate-700">
                خيارات الخبز أو الأحجام (اختياري):
              </label>

              {/* Add Variant Row */}
              <div className="flex gap-2">
                <input
                  type="text"
                  value={varName}
                  onChange={(e) => setVarName(e.target.value)}
                  placeholder="اسم الخيار (صمون / صاج / فرنساوي)"
                  className="flex-2 text-xs bg-slate-50 border border-slate-200 rounded-xl px-3 py-2"
                />
                <input
                  type="number"
                  value={varPrice}
                  onChange={(e) => setVarPrice(e.target.value)}
                  placeholder="السعر"
                  className="flex-1 text-xs bg-slate-50 border border-slate-200 rounded-xl px-3 py-2"
                />
                <button
                  type="button"
                  onClick={handleAddVariant}
                  className="px-3 py-2 bg-slate-800 hover:bg-slate-700 text-white text-xs font-bold rounded-xl"
                >
                  إضافة
                </button>
              </div>

              {/* Variants list tags */}
              {formData.variants.length > 0 && (
                <div className="flex gap-1.5 flex-wrap pt-1">
                  {formData.variants.map((v) => (
                    <span
                      key={v.id}
                      className="px-2 py-1 bg-red-50 text-red-800 border border-red-200 rounded-lg text-xs font-bold flex items-center gap-1.5"
                    >
                      <span>
                        {v.name}: {v.price} ج
                      </span>
                      <button
                        type="button"
                        onClick={() => handleRemoveVariant(v.id)}
                        className="text-red-500 hover:text-red-700"
                      >
                        ×
                      </button>
                    </span>
                  ))}
                </div>
              )}
            </div>

            <div className="flex gap-2 pt-2">
              <button
                type="button"
                onClick={() => setModalOpen(false)}
                className="flex-1 py-2.5 bg-slate-100 hover:bg-slate-200 text-slate-700 text-xs font-bold rounded-xl"
              >
                إلغاء
              </button>
              <button
                type="submit"
                className="flex-1 py-2.5 bg-red-600 hover:bg-red-700 text-white text-xs font-bold rounded-xl shadow-md"
              >
                حفظ الصنف
              </button>
            </div>
          </form>
        </div>
      )}
    </div>
  );
}
