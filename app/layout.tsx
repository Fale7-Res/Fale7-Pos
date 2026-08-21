import type { Metadata } from 'next';
import './globals.css';

export const metadata: Metadata = {
  title: 'فاتح أبو الغنية - نظام إدارة ونقاط بيع المطاعم',
  description: 'نظام إدارة ونقاط بيع المطاعم المتكامل لمطعم فاتح أبو الغنية (فالح - أبو العنبة - 24 ساعة)',
};

export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="ar" dir="rtl">
      <body className="bg-[#F5F5F5] text-slate-900 antialiased selection:bg-blue-600 selection:text-white min-h-screen">
        {children}
      </body>
    </html>
  );
}
