import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';

import { EmployeesPageComponent } from './employees/employees-page.component';
import { ShiftsPageComponent } from './shifts/shifts-page.component';

const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'employees' },
  { path: 'employees', component: EmployeesPageComponent },
  { path: 'shifts', component: ShiftsPageComponent },
  { path: '**', redirectTo: 'employees' }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
