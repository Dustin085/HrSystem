import { Component, OnInit } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { finalize } from 'rxjs';

import { CreateEmployeeRequest, EmployeeDto, EmployeesService } from '../api';
import { toErrorMessages } from '../shared/api-errors';
import { todayIso } from '../shared/dates';

@Component({
  selector: 'app-employees-page',
  templateUrl: './employees-page.component.html'
})
export class EmployeesPageComponent implements OnInit {
  employees: EmployeeDto[] = [];
  errors: string[] = [];
  saving = false;

  form = this.fb.group({
    name: ['', [Validators.required, Validators.maxLength(50)]],
    hireDate: [todayIso(), Validators.required]
  });

  constructor(
    private fb: FormBuilder,
    private employeesService: EmployeesService
  ) { }

  ngOnInit(): void {
    this.loadEmployees();
  }

  loadEmployees(): void {
    this.employeesService.listEmployees().subscribe({
      next: employees => this.employees = employees,
      error: error => this.errors = toErrorMessages(error)
    });
  }

  submit(): void {
    if (this.form.invalid) {
      // 錯誤訊息只在欄位「被碰過」(touched) 時才顯示（見 isInvalid）。
      // 使用者什麼都沒填就直接按送出時，欄位都還沒被碰過，畫面上不會有任何提示，
      // 所以這裡把所有欄位都標成 touched，讓錯誤訊息一次全部顯示出來。
      this.form.markAllAsTouched();
      return; // 表單不合法就不送出請求
    }

    const request: CreateEmployeeRequest = this.form.value;

    this.saving = true;
    this.errors = [];

    this.employeesService.createEmployee(request)
      .pipe(finalize(() => this.saving = false))
      .subscribe({
        next: () => {
          // reset 會同時清掉 touched / dirty 狀態，表單回到剛打開時的樣子（不會顯示錯誤）
          this.form.reset({ name: '', hireDate: todayIso() });
          this.loadEmployees();
        },
        error: error => this.errors = toErrorMessages(error)
      });
  }

  /**
   * 欄位是否要顯示錯誤：值不合法「而且」使用者碰過這個欄位。
   * 加上 touched 條件，是為了避免一打開頁面，空白的必填欄位就滿版紅字。
   * touched 會在使用者點進欄位再離開 (blur) 時變成 true，或由 markAllAsTouched() 一次設定。
   */
  isInvalid(controlName: string): boolean {
    const control = this.form.get(controlName);
    return !!control && control.invalid && control.touched;
  }
}
