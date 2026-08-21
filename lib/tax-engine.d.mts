import type { TaxRule } from './enterprise-types';
export function calculateTax(amountPiastres: number, rule?: TaxRule): { taxable: number; tax: number; final: number };
