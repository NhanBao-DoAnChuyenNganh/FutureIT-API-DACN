using DoAnCoSo_Web.Models;
using DoAnCoSo_Web.Models.Momo;

namespace DoAnCoSo_Web.Areas.Student.MomoService
{
    public interface IMomoService
    {
        Task<MomoCreatePaymentResponseModel> CreatePaymentMomo(OrderInfo model);
        MomoExecuteResponseModel PaymentExecuteAsync(IQueryCollection collection);
    }
}
