export interface IProduct {
  Name: string;
  Barcode: string;
  Description: string;
  Rate: number;
  Price: number;
  Id: number;
  ProductId: string;
  PhysicalQty: number;
  ReservedQty: number;
  AvailableQty: number;
  CreatedBy: string;
  Created: string;
  LastModifiedBy: string;
  LastModified: string;
  IsActive: boolean;
  ImageUrl?: any;
}
