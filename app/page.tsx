'use client';

import React from 'react';
import { AppProvider, useAppStore } from '@/lib/store';
import { EnterpriseProvider } from '@/lib/enterprise-store';
import AppShell from '@/components/layout/AppShell';
import PersistenceGuard from '@/components/PersistenceGuard';

// Feature Screens
import PosScreen from '@/features/pos/PosScreen';
import GrillScreen from '@/features/grill/GrillScreen';
import OrdersScreen from '@/features/orders/OrdersScreen';
import DashboardScreen from '@/features/dashboard/DashboardScreen';
import ShiftsScreen from '@/features/shifts/ShiftsScreen';
import DailySummaryScreen from '@/features/reports/DailySummaryScreen';
import DailyWagesPanel from '@/features/employees/DailyWagesPanel';
import EmployeesScreen from '@/features/employees/EmployeesScreen';
import AttendanceScreen from '@/features/attendance/AttendanceScreen';
import ExpensesScreen from '@/features/expenses/ExpensesScreen';
import CashScreen from '@/features/cash/CashScreen';
import ControlCenterScreen from '@/features/control-center/ControlCenterScreen';
import AuditLogScreen from '@/features/audit/AuditLogScreen';
import MenuManagementScreen from '@/features/menu/MenuManagementScreen';
import UsersPermissionsScreen from '@/features/users/UsersPermissionsScreen';
import ReportsScreen from '@/features/reports/ReportsScreen';
import DeviceManagementScreen from '@/features/devices/DeviceManagementScreen';
import BackupScreen from '@/features/backup/BackupScreen';
import SettingsScreen from '@/features/settings/SettingsScreen';
import TreasuryScreen from '@/features/treasury/TreasuryScreen';
import DailySettlementScreen from '@/features/treasury/DailySettlementScreen';
import PurchasingScreen from '@/features/purchasing/PurchasingScreen';
import InventoryScreen from '@/features/inventory/InventoryScreen';
import PayrollHrScreen from '@/features/hr/PayrollHrScreen';
import TaxScreen from '@/features/tax/TaxScreen';
import DeliveryTablesScreen from '@/features/delivery/DeliveryTablesScreen';

// Modals
import ManagerAuthModal from '@/components/modals/ManagerAuthModal';
import LockScreenModal from '@/components/modals/LockScreenModal';
import ReceiptModal from '@/components/modals/ReceiptModal';

function AppContent() {
  const { activeModule } = useAppStore();

  const renderActiveScreen = () => {
    switch (activeModule) {
      case 'pos':
        return <PosScreen />;
      case 'grill':
        return <GrillScreen />;
      case 'orders':
        return <OrdersScreen />;
      case 'dashboard':
        return <DashboardScreen />;
      case 'shifts':
        return <ShiftsScreen />;
      case 'daily_summary':
        return <DailySummaryScreen />;
      case 'daily_wages':
        return <DailyWagesPanel />;
      case 'employees':
        return <EmployeesScreen />;
      case 'attendance':
        return <AttendanceScreen />;
      case 'expenses':
        return <ExpensesScreen />;
      case 'cash':
        return <CashScreen />;
      case 'treasury':
        return <TreasuryScreen />;
      case 'settlement':
        return <DailySettlementScreen />;
      case 'purchasing':
        return <PurchasingScreen />;
      case 'inventory':
        return <InventoryScreen />;
      case 'payroll_hr':
        return <PayrollHrScreen />;
      case 'tax':
        return <TaxScreen />;
      case 'delivery':
        return <DeliveryTablesScreen />;
      case 'control_center':
        return <ControlCenterScreen />;
      case 'audit':
        return <AuditLogScreen />;
      case 'menu':
        return <MenuManagementScreen />;
      case 'users':
        return <UsersPermissionsScreen />;
      case 'reports':
        return <ReportsScreen />;
      case 'devices':
        return <DeviceManagementScreen />;
      case 'backup':
        return <BackupScreen />;
      case 'settings':
        return <SettingsScreen />;
      default:
        return <PosScreen />;
    }
  };

  return (
    <AppShell>
      {renderActiveScreen()}
      <ManagerAuthModal />
      <LockScreenModal />
      <ReceiptModal />
    </AppShell>
  );
}

export default function Home() {
  return (
    <EnterpriseProvider>
      <AppProvider>
        <PersistenceGuard><AppContent /></PersistenceGuard>
      </AppProvider>
    </EnterpriseProvider>
  );
}
