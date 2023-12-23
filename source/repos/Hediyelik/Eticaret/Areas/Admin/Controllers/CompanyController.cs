using Hediyelik.DataAccess.Repository.IRepository;
using Hediyelik.Models;
using Hediyelik.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Eticaret.Areas.Admin.Controllers;
[Area("Admin")]
public class CompanyController : Controller
{
    private readonly IUnitOfWork _unitOfWork; 
    public CompanyController(IUnitOfWork unit)
    {
        _unitOfWork = unit;        
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Upsert(int? id)
    {
        Company company = new();       


        if (id == null || id == 0)
        {
            return View(company);
        }
        else
        {
            company = _unitOfWork.Company.GetFirstOrDefault(u=>u.Id==id);
            return View(company);
        }

        
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Upsert(Company obj)
    {
        if (ModelState.IsValid)
        {            
            
            if(obj.Id == 0)
            {
                _unitOfWork.Company.Add(obj);
                TempData["success"] = "Company Creeated Successfully";
            }
            else {
                _unitOfWork.Company.Update(obj);
                TempData["success"] = "Company Updated Successfully";
            }
            
            _unitOfWork.Save();
            
            return RedirectToAction("Index");
        }

        return View(obj);
    }

    #region API CALLS
    [HttpGet]
    public IActionResult GetAll()
    {
        var companyList = _unitOfWork.Company.GetAll();
        return Json(new
        {
            data = companyList
        });
    }
    [HttpDelete]
    public IActionResult Delete(int? id)
    {
        var obj = _unitOfWork.Company.GetFirstOrDefault(u => u.Id == id);

        if (obj == null)
        {
            return Json(new { success = false, message = "Error while deleting" });
        }

        _unitOfWork.Company.Remove(obj);
        _unitOfWork.Save();

        return Json(new { success = true, message = "Delete successfull" });
    }


    #endregion


}
