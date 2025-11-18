using DoAnCoSo_Web.Models.Vnpay;

namespace DoAnCoSo_Web.Areas.Student.VnpayService
{
    public interface IVnPayService
    {
        string CreatePaymentUrl(PaymentInformationModel model, HttpContext context);
        PaymentResponseModel PaymentExecute(IQueryCollection collections);

    }
}
