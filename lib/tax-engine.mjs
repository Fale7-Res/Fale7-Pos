export function calculateTax(amountPiastres, rule) {
  if (!rule?.enabled || rule.rateBasisPoints <= 0) return { taxable: amountPiastres, tax: 0, final: amountPiastres };
  if (rule.mode === 'inclusive') {
    const tax = Math.round(amountPiastres * rule.rateBasisPoints / (10000 + rule.rateBasisPoints));
    return { taxable: amountPiastres - tax, tax, final: amountPiastres };
  }
  const tax = Math.round(amountPiastres * rule.rateBasisPoints / 10000);
  return { taxable: amountPiastres, tax, final: amountPiastres + tax };
}
