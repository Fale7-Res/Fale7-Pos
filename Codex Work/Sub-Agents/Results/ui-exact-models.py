from ui_source_receipts import *
persist_exact(C+'Models/CartLine.cs',{
(14,40):('Returns immutable product ID copied to station checkout item; display names never used as authority. No stock or payment side effect.','CallCenterVM.SubmitOrderAsync projection','Backend verifies product availability.'),
(14,45):('Initializes required product identity from selected menu DTO, with no runtime nonempty validation. No setter permits retargeting existing line.','CallCenterVM.AddToCart','Malformed menu ID contract.'),
(15,32):('Returns optional variant ID for checkout; null means no variant. Getter cannot prove parent-product relation.','CheckoutItemRequest','Backend wrong-variant rejection fixture.'),
(15,37):('Initializes variant identity once from chosen variant. Quantity and notes mutations cannot alter it afterward.','CallCenterVM.AddToCart','Stale variant after menu reload.'),
(16,42):('Returns product label from menu snapshot for cart display, not authoritative checkout product name.','Cart binding caller exact UNKNOWN','Long Arabic label native layout unobserved.'),
(16,47):('Initializes required display name from menu and retains snapshot through draft lock/unlock. Server rename does not automatically update existing cart.','CallCenterVM.AddToCart','Menu rename versus retained draft.'),
(17,34):('Returns optional variant display label, independent from request variant identity. Null label is ordinary unvarianted row.','Cart row display','Native null label spacing.'),
(17,39):('Initializes variant label once; no subsequent notification or catalog rename subscription. No network activity.','CallCenterVM.AddToCart','Display/identity remain distinct.'),
(18,31):('Returns cached preparation flag, while station checkout sends product identity only, not this flag. Server decides kitchen route.','CartLine.SendToGrill; checkout projection omits it','Do not claim client controls ticket routing.'),
(18,36):('Initializes preparation flag from menu snapshot for client presentation. Server route can change afterward independently.','CallCenterVM.AddToCart','Routing change before checkout server contract.'),
(19,37):('Returns cached preview price. LineTotal uses unchecked long multiplication with quantity, and checkout never sends unit price.','LineTotalPiastres; UnitPriceDisplay','Extreme overflow and authoritative price changed test.'),
(19,42):('Initializes offer-fixed/variant/base selected preview price once. Menu reload does not reprice existing lines automatically.','CallCenterVM.AddToCart unitPrice','Stale estimate versus backend final amount UX.'),
(20,37):('Returns applied offer ID included in checkout; no verification offer still active at settlement.','SubmitOrderAsync item projection','Expired offer backend revalidation.'),
(20,42):('Initializes optional matched offer identity from selected product offer list. Immutable while line quantity changes.','CallCenterVM.AddToCart','Foreign offer ID direct-call fixture.'),
(21,39):('Returns optional offer label for badge, without contributing to request ID or pricing itself.','Cart row offer label','Native badge contrast unobserved.'),
(21,44):('Initializes offer label snapshot; null for ordinary item, duplicate names do not merge identities.','CallCenterVM.AddToCart','Renamed/missing offer display semantics.'),
(33,5):('Quantity callback notifies line total/display only. Parent sum is explicitly recomputed by VM commands; direct binding quantity edits can leave parent stale unless view compensates. No quantity cap or checked multiplication.','OnPropertyChanged line total fields; parent quantity commands','Trace actual view input callback and int/long bounds.')
},'Read complete CallCenter CartLine independently; all17 exact get/init/callback spans reconciled, same preview-only arithmetic contract as Cashier.')
for base,offset in ((P,0),(C,-1)):
 persist_exact(base+'Models/ProductTile.cs',{
 (24+offset,85):('Projects each variant PricePiastres to Min for starting-price badge. With>1 variants prepends from, zero variants base price. Does not account offer fixed price in starting label, but separate offer buttons/banner explain deal.','StartingPriceDisplay -> Product.Variants.Min','Min/base/offer label clarity; backend authoritative pricing.'),
 (44+offset,39):('Returns immutable category DTO wrapped by observable selection flag. VM selection compares ReferenceEquals to this object, so identity reference matters to chip highlight.','OnSelectedCategoryChanged; SelectCategory','Equal cloned DTO versus original reference fixture.'),
 (45+offset,5):('Constructor retains category without network or event subscription. ProductTile and OfferChoice records pair product/offer immutably; expression HasImage/FallbackIcon/StationLabel inspected but not counted as separate callable declarations by map.','CategoryTile Category assignment','Null category contract; native fallback images and RTL layout not observed.')
 },'Read complete ProductTile/CategoryTile source per client; all3 indexed callable spans reconciled plus expression resource/display responsibilities. Cashier IsBestSeller flag differs from CallCenter.')
