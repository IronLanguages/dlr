# Getting Started with the DLR as a Library Author

## Contents

- [Frontmatter](frontmatter.md)
- [1 Introduction](introduction.md)
- [2 ExpandoObject](expandoobject.md)
- [3 DynamicObject](dynamicobject.md)
	- [3.1 DynamicBag: Implementing Our Own ExpandoObject](dynamicobject.md#dynamicbag-implementing-our-own-expandoobject)
	- [3.2 NamedBag: Optimizing DynamicObject with Statically Defined Members](dynamicobject.md#namedbag-optimizing-dynamicobject-with-statically-defined-members)
- [4 IDynamicMetaObjectProvider and DynamicMetaObject](idynamicmetaobjectprovider-and-dynamicmetaobject.md)
	- [4.1 FastNBag: Faster Bags for N Slots](idynamicmetaobjectprovider-and-dynamicmetaobject.md#fastnbag-faster-bags-for-n-slots)
		- [4.1.1 BindSetMember Method](idynamicmetaobjectprovider-and-dynamicmetaobject.md#bindsetmember-method)
		- [4.1.2 BindGetMember Method](idynamicmetaobjectprovider-and-dynamicmetaobject.md#bindgetmember-method)
		- [4.1.3 GetDynamicMemberNames Method](idynamicmetaobjectprovider-and-dynamicmetaobject.md#getdynamicmembernames-method)
	- [4.2 Further Reading](idynamicmetaobjectprovider-and-dynamicmetaobject.md#further-reading)
- [5 Appendix](appendix.md)
	- [5.1 DynamicObject Virtual Methods](appendix.md#dynamicobject-virtual-methods)
	- [5.2 FastNBag Full Source](appendix.md#fastnbag-full-source)

## All Documents

[Original Documentation](../)
- [DLR Overview](../dlr-overview/)
- [DLR Hosting Spec](../dlr-spec-hosting/)
- [Expression Trees v2 Spec](../expr-tree-spec/)
- [Getting Started with the DLR as a Library Author](../library-authors-introduction/)
- [Sites, Binders, and Dynamic Object Interop Spec](../sites-binders-dynobj-interop/)
- [SymPL Implementation on the Dynamic Language Runtime](../sympl/)
